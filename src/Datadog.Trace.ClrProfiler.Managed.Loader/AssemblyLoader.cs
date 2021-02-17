using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Datadog.AutoInstrumentation.ManagedLoader
{
    /// <summary>
    /// Loads specified assemblies into the current AppDomain.
    ///
    /// This is the only public class in this assembly.
    /// This entire assembly is compiled into the native profiler DLL as a resource.
    /// This happens both, for the profiler part of the Tracer, and for the actual Profiler.
    /// The native code then uses this class to call arbitrary managed code:
    /// It uses IL rewriting to inject a call to <c>AssemblyLoader.Run(..)</c> and passes a list of assemblies.
    /// Then, this class loads all of the specified assemblies and calls a well known entry point in those assemblies.
    /// (See <c>TargetLibraryEntrypointXxx</c> constants in this class.)
    /// If the specified assemblies do not contain such an entry point, they will be loaded, but nothing will be executed.
    ///
    /// This class sets up a basic AppDomain.AssemblyResolve handler to look for the assemblies in a framework-specific subdirectory
    /// of the product home directory in addition to the normal probing paths (e.g. DD_DOTNET_TRACER_HOME for the Tracer).
    /// It also allows for some SxS loading using custom Assembly Load Context.
    ///
    /// If a target assembly needs additional AssemblyResolve event logic to satisfy its dependencies,
    /// or for any other reasons, it must set up its own AssemblyResolve handler as the first thing after its
    /// entry point is called.
    ///
    /// ! Do not make the AppDomain.AssemblyResolve handler in here more complex !
    /// If anything, it should be simplified and any special logic should be moved into the respective assemblies
    /// requested for loading.
    ///
    /// ! Also, remember that this assembly is shared between the Tracer's profiler component
    /// and the Profiler's profiler component. Do not put specialized code here !
    /// </summary>
    public partial class AssemblyLoader
    {
        /// <summary>
        /// The constants <c>TargetLibraryEntrypointMethod</c>, and <c>...Type</c> specify
        /// which entrypoint to call in the specified assemblies. The respective assembly is expected to have
        /// exactly this entry point. Otherwise, the assembly will be loaded, but nothing will be invoked
        /// explicitly (but be aware of Module cctor caveats).
        /// The method must be static, the return type of the method must be <c>void</c> and it must have no parameters.
        /// Before doing anything else, the target assemblies must set up AppDomain AssemblyResolve events that
        /// make sure that their respective dependencies can be loaded.
        /// </summary>
        public const string TargetLibraryEntrypointMethod = "Run";

        /// <summary> The namespace and the type name of the entrypoint to invoke in each loaded assemby.
        /// More info: <see cref="AssemblyLoader.TargetLibraryEntrypointMethod" />. </summary>
        public const string TargetLibraryEntrypointType = "Datadog.AutoInstrumentation" + "." + "DllMain";

        internal const string AssemblyLoggingComponentMonikerPrefix = "ManagedLoader.";
        private const string LoggingComponentMoniker = AssemblyLoader.AssemblyLoggingComponentMonikerPrefix + nameof(AssemblyLoader);

        private readonly string[] _assemblyNamesToLoad;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyLoader"/> class.
        /// </summary>
        /// <param name="assemblyNamesToLoad">List of assemblies to load and start.</param>
        public AssemblyLoader(string[] assemblyNamesToLoad)
        {
            _assemblyNamesToLoad = assemblyNamesToLoad;
        }

        /// <summary>
        /// Instantiates an <c>AssemblyLoader</c> instance with the specified assemblies and executes it.
        /// </summary>
        /// <param name="assemblyNamesToLoad">List of assemblies to load ans start.</param>
        public static void Run(string[] assemblyNamesToLoad)
        {
            try
            {
                try
                {
                    var assemblyLoader = new AssemblyLoader(assemblyNamesToLoad);
                    assemblyLoader.Execute();
                }
                catch (Exception ex)
                {
                    Log.Error(LoggingComponentMoniker, ex);
                }
            }
            catch
            {
                // An exception must have come out of the logger.
                // We swallow it to avpid crashing the process.
            }
        }

        /// <summary>
        /// Loads the assemblied specified for this <c>AssemblyLoader</c> instance and executes their entry point.
        /// </summary>
        public void Execute()
        {
            Log.Info(LoggingComponentMoniker, "Initializing");

            AssemblyResolveEventHandler assemblyResolveEventHandler = CreateAssemblyResolveEventHandler();
            if (assemblyResolveEventHandler == null)
            {
                return;
            }

            Log.Info(LoggingComponentMoniker, "Registering AssemblyResolve handler");

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += assemblyResolveEventHandler.OnAssemblyResolve;
            }
            catch (Exception ex)
            {
                Log.Error(LoggingComponentMoniker, "Unable to register an AssemblyResolve event handler", ex);
            }

            var logEntryDetails = new List<object>();

            logEntryDetails.Add("Number of assemblies");
            logEntryDetails.Add(assemblyResolveEventHandler.AssemblyNamesToLoad.Count);
            logEntryDetails.Add("Number of product binaries directories");
            logEntryDetails.Add(assemblyResolveEventHandler.ManagedProductBinariesDirectories.Count);

            for (int i = 0; i < assemblyResolveEventHandler.ManagedProductBinariesDirectories.Count; i++)
            {
                logEntryDetails.Add($"managedProductBinariesDirectories[{i}]");
                logEntryDetails.Add(assemblyResolveEventHandler.ManagedProductBinariesDirectories[i]);
            }

            Log.Info(LoggingComponentMoniker, "Starting to load assemblies", logEntryDetails);

            for (int i = 0; i < assemblyResolveEventHandler.AssemblyNamesToLoad.Count; i++)
            {
                string assemblyName = assemblyResolveEventHandler.AssemblyNamesToLoad[i];

                try
                {
                    LoadAndStartAssembly(assemblyName);
                }
                catch (Exception ex)
                {
                    Log.Error(LoggingComponentMoniker, "Error loading or starting a managed assembly", ex, "assemblyName", assemblyName);
                }
            }
        }

        private static void LoadAndStartAssembly(string assemblyName)
        {
            // We have previously excluded assembly names that are null or white-space.
            assemblyName = assemblyName.Trim();

            Log.Info(LoggingComponentMoniker, "Loading managed assembly", "assemblyName", assemblyName);

            Assembly assembly = Assembly.Load(assemblyName);
            if (assembly == null)
            {
                Log.Error(LoggingComponentMoniker, "Could not load managed assembly", "assemblyName", assemblyName);
                return;
            }

            Type entryPointType = assembly.GetType(TargetLibraryEntrypointType, throwOnError: false);
            if (entryPointType == null)
            {
                Log.Info(
                        LoggingComponentMoniker,
                        "Assembly was loaded, but entry point was not invoked, bacause it does not contain the entry point type",
                        "assembly",
                        assembly.FullName,
                        "entryPointType",
                        TargetLibraryEntrypointType);
                return;
            }

            MethodInfo entryPointMethod = entryPointType.GetRuntimeMethod(TargetLibraryEntrypointMethod, parameters: new Type[0]);
            if (entryPointMethod == null)
            {
                Log.Info(
                        LoggingComponentMoniker,
                        "Assembly was loaded, but entry point was not invoked, bacause the entry point type does not contain the entry point method",
                        "assembly",
                        assembly.FullName,
                        "entryPointType",
                        entryPointType.FullName,
                        "entryPointMethod",
                        TargetLibraryEntrypointMethod);
                return;
            }

            try
            {
                entryPointMethod.Invoke(obj: null, parameters: null);
            }
            catch (Exception ex)
            {
                Log.Error(
                        LoggingComponentMoniker,
                        "Assembly was loaded and the entry point was invoked; an exception was thrown from the entry point",
                        ex,
                        "assembly",
                        assembly.FullName,
                        "entryPointType",
                        entryPointType.FullName,
                        "entryPointMethod",
                        entryPointMethod.Name);
                return;
            }

            Log.Info(
                    LoggingComponentMoniker,
                    "Assembly was loaded and the entry point was invoked",
                    "assembly",
                    assembly.FullName,
                    "entryPointType",
                    entryPointType.FullName,
                    "entryPointMethod",
                    entryPointMethod.Name);
            return;
        }

        private static IReadOnlyList<string> CleanAssemblyNamesToLoad(string[] assemblyNamesToLoad)
        {
            if (assemblyNamesToLoad == null)
            {
                Log.Info(LoggingComponentMoniker, $"Not loading any assemblies ({nameof(assemblyNamesToLoad)} is null). ");
                return null;
            }

            if (assemblyNamesToLoad.Length == 0)
            {
                Log.Info(LoggingComponentMoniker, $"Not loading any assemblies ({nameof(assemblyNamesToLoad)}.{nameof(assemblyNamesToLoad.Length)} is 0). ");
                return null;
            }

            // Check for bad assemblyNamesToLoad entries. We expect the array to be small and entries to be OK.
            // So scrolling multiple times is better then allocating a temp buffer.
            int validAssemblyNamesCount = 0;
            for (int pAsmNames = 0; pAsmNames < assemblyNamesToLoad.Length; pAsmNames++)
            {
                if (!string.IsNullOrWhiteSpace(assemblyNamesToLoad[pAsmNames]))
                {
                    validAssemblyNamesCount++;
                }
            }

            if (validAssemblyNamesCount == 0)
            {
                Log.Info(LoggingComponentMoniker, $"Not loading any assemblies. Some assembly names were specified, but they are all null or white-space.");
                return null;
            }

            if (assemblyNamesToLoad.Length == validAssemblyNamesCount)
            {
                return assemblyNamesToLoad;
            }

            var validAssemblyNamesToLoad = new string[validAssemblyNamesCount];
            for (int pAsmNames = 0, pValidAsmNames = 0; pAsmNames < assemblyNamesToLoad.Length; pAsmNames++)
            {
                if (!string.IsNullOrWhiteSpace(assemblyNamesToLoad[pAsmNames]))
                {
                    validAssemblyNamesToLoad[pValidAsmNames++] = assemblyNamesToLoad[pAsmNames];
                }
            }

            return validAssemblyNamesToLoad;
        }

        private static IReadOnlyList<string> ResolveManagedProductBinariesDirectories()
        {
            var binaryDirs = new List<string>(capacity: 3);

            string tracerDir = GetTracerManagedBinariesDirectory();
            if (tracerDir != null)
            {
                binaryDirs.Add(tracerDir);
            }

            string profilerDir = GetProfilerManagedBinariesDirectory();
            if (profilerDir != null)
            {
                binaryDirs.Add(profilerDir);
            }

            return binaryDirs;
        }

        private static string GetTracerManagedBinariesDirectory()
        {
            // E.g.:
            //  - c:\Program Files\Datadog\.NET Tracer\net45\
            //  - c:\Program Files\Datadog\.NET Tracer\netcoreapp3.1\
            //  - ...

            string tracerHomeDirectory = ReadEnvironmentVariable("DD_DOTNET_TRACER_HOME") ?? string.Empty;
            string managedBinariesSubdir = GetRuntimeBasedProductBinariesSubdir();
            string managedBinariesDirectory = Path.Combine(tracerHomeDirectory, managedBinariesSubdir);

            return managedBinariesDirectory;
        }

        private static string GetProfilerManagedBinariesDirectory()
        {
            // Assumed folder structure (may change while we ar still in Alpha):
            //  - c:\Program Files\Datadog\.NET Tracer\                                     <= Native Tracer/Provifer loader binary
            //  - c:\Program Files\Datadog\.NET Tracer\                                     <= Also, native Tracer binaries for Win-x64
            //  - c:\Program Files\Datadog\.NET Tracer\net45\                               <= Managed Tracer binaries for Net Fx 4.5
            //  - c:\Program Files\Datadog\.NET Tracer\netcoreapp3.1\                       <= Managed Tracer binaries for Net Core 3.1 +
            //  - c:\Program Files\Datadog\.NET Tracer\ContinuousProfiler\win-x64\          <= Native Profiler binaries for Win-x64
            //  - c:\Program Files\Datadog\.NET Tracer\ContinuousProfiler\net45\            <= Managed Profiler binaries for Net Fx 4.5
            //  - c:\Program Files\Datadog\.NET Tracer\ContinuousProfiler\netcoreapp3.1\    <= Managed Profiler binaries for Net Core 3.1 +
            //  - ...

            string managedBinariesSubdir = GetRuntimeBasedProductBinariesSubdir(out bool isCoreFx);

            string nativeProductBinariesDir;
            if (isCoreFx)
            {
                nativeProductBinariesDir = ReadEnvironmentVariable(Environment.Is64BitProcess ? "CORECLR_PROFILER_PATH_64" : "CORECLR_PROFILER_PATH_32");
                nativeProductBinariesDir = nativeProductBinariesDir ?? ReadEnvironmentVariable("CORECLR_PROFILER_PATH");
            }
            else
            {
                nativeProductBinariesDir = ReadEnvironmentVariable(Environment.Is64BitProcess ? "COR_PROFILER_PATH_64" : "COR_PROFILER_PATH_32");
                nativeProductBinariesDir = nativeProductBinariesDir ?? ReadEnvironmentVariable("COR_PROFILER_PATH");
            }

            nativeProductBinariesDir = nativeProductBinariesDir ?? string.Empty;                            // Be defensive against env var not being set
            nativeProductBinariesDir = Path.GetDirectoryName(Path.Combine(nativeProductBinariesDir, "."));  // Normalize in respect to final dir separator

            string tracerHomeDirectory = Path.GetDirectoryName(nativeProductBinariesDir);                   // Shared Tracer/Provifer loader is in Tracer HOME
            string profilerHomeDirectory = Path.Combine(tracerHomeDirectory, "ContinuousProfiler");         // Profiler-HOME is in <Tracer-HOME>/ContinuousProfiler

            string managedBinariesDirectory = Path.Combine(profilerHomeDirectory, managedBinariesSubdir);   // Managed binarie are in <Profiler-HOME>/net-ver-moniker/
            return managedBinariesDirectory;
        }

        private static string GetRuntimeBasedProductBinariesSubdir()
        {
            return GetRuntimeBasedProductBinariesSubdir(out bool _);
        }

        private static string GetRuntimeBasedProductBinariesSubdir(out bool isCoreFx)
        {
            Assembly objectAssembly = typeof(object).Assembly;
            isCoreFx = (objectAssembly?.FullName?.StartsWith("System.Private.CoreLib") == true);

            string productBinariesSubdir;
            if (isCoreFx)
            {
                // We are running under .NET Core (or .NET 5+).
                // Old versions of .NET core report a major version of 4.
                // The respective binaries are in <HOME>/netstandard2.0/...
                // Newer binaries are in <HOME>/netcoreapp3.1/...
                // This needs to be extended if and when we ship a specific distro for newer .NET versions!

                Version clrVersion = Environment.Version;
                if ((clrVersion.Major == 3 && clrVersion.Minor >= 1) || clrVersion.Major >= 5)
                {
                    productBinariesSubdir = "netcoreapp3.1";
                }
                else
                {
                    productBinariesSubdir = "netstandard2.0";
                }
            }
            else
            {
                // We are running under the (classic) .NET Framework.
                // We currently ship two distributions targeting .NET Framework.
                // We want to use the highest-possible compatible assembly version number.
                // We will try getting the version of mscorlib used.
                // If that version is >= 4.61 then we use the respective distro. Otherwise we use the Net F 4.5 distro.

                try
                {
                    string objectAssemblyFileVersionString = ((AssemblyFileVersionAttribute)objectAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
                    var objectAssemblyVersion = new Version(objectAssemblyFileVersionString);

                    var mscorlib461Version = new Version("4.6.1055.0");

                    productBinariesSubdir = (objectAssemblyVersion < mscorlib461Version) ? "net45" : "net461";
                }
                catch
                {
                    productBinariesSubdir = "net45";
                }
            }

            return productBinariesSubdir;
        }

        private static string ReadEnvironmentVariable(string envVarName)
        {
            try
            {
                return Environment.GetEnvironmentVariable(envVarName);
            }
            catch (Exception ex)
            {
                Log.Error(LoggingComponentMoniker, "Error while loading environment variable", ex, "envVarName", envVarName);
                return null;
            }
        }

        private AssemblyResolveEventHandler CreateAssemblyResolveEventHandler()
        {
            IReadOnlyList<string> assemblyNamesToLoad = CleanAssemblyNamesToLoad(_assemblyNamesToLoad);
            if (assemblyNamesToLoad == null)
            {
                return null;
            }

            IReadOnlyList<string> managedProductBinariesDirectories = ResolveManagedProductBinariesDirectories();

            var assemblyResolveEventHandler = new AssemblyResolveEventHandler(assemblyNamesToLoad, managedProductBinariesDirectories);
            return assemblyResolveEventHandler;
        }
    }
}