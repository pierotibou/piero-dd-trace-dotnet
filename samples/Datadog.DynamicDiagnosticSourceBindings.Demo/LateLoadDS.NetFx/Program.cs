﻿using DynamicDiagnosticSourceBindings.Demo;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.LateLoadDS.NetFx
{
    public class Program
    {
        // This application uses DiagnosticSource.dll but it uses it AFTER we use the stubbs first.
        // So we expect that the stubbs will look for DS on the standard probing path, WILL find it and use it.
        // This can be validated via logs.
        // In order to validate the correct behaviour where an application did something very custom and
        // used DS where is it NOT on th eprobing path, move the DS.dll into the following directory.
        // This demo uses an assembly resolve event handler to allo looking in this path only phase one is completed.
        // The expected behaviour is then that the stubbs first use the vendored-in version and then dynamically switch to
        // the normal DS once it has been loaded.
        // This can also be validated using the logs.
        private const string DiagnosticSourceAssemblyFilename = "System.Diagnostics.DiagnosticSource.dll";
        private const string DiagnosticSourceAssemblyHiddenPath = "./HiddenAssemblies/";

        // Set to true to automatically move the DS file at the start of the app to automate the validation described above.
        private const bool HideDiagnosticSourceAssembly = true;

        private int _isPhaseOneCompleted = 0;

        public static void Main(string[] args)
        {
            (new Program()).Run();
        }

        public void Run()
        {
            const int MaxIterations = 1000;
            const int PhaseOneIterations = 300;

            const int ReceivedEventsVisualWidth = 100;

            Console.WriteLine();
            Console.WriteLine($"Welcome to {this.GetType().FullName} in {Process.GetCurrentProcess().ProcessName}");

            Console.WriteLine();
            Console.WriteLine($"{nameof(HideDiagnosticSourceAssembly)} = {HideDiagnosticSourceAssembly}");

#pragma warning disable CS0162 // Unreachable code detected: intentional controll via a const bool.
            if (HideDiagnosticSourceAssembly)
            {
                string destination = Path.Combine(DiagnosticSourceAssemblyHiddenPath, DiagnosticSourceAssemblyFilename);

                try
                {
                    Directory.CreateDirectory(DiagnosticSourceAssemblyHiddenPath);

                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                }
                catch { }

                File.Move(DiagnosticSourceAssemblyFilename, destination);

                Console.WriteLine($"Moved \"{DiagnosticSourceAssemblyFilename}\" to \"{destination}\".");

                Console.WriteLine();
                Console.WriteLine($"Setting up the AssemblyResolve handler for the current AppDomain.");

                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
            }
            else
            {
                Console.WriteLine("Did not hide the DS assembly.");
            }
#pragma warning restore CS0162 // Unreachable code detected

            Console.WriteLine();
            Console.WriteLine($"Setting up {nameof(StubbedDiagnosticEventsCollector)}.");

            var directResultsAccumulator = new ReceivedEventsAccumulator(MaxIterations);
            var stubbedResultsAccumulator = new ReceivedEventsAccumulator(MaxIterations);
            var stubbedCollector = new StubbedDiagnosticEventsCollector(directResultsAccumulator, stubbedResultsAccumulator);

            Console.WriteLine();
            Console.WriteLine($"Starting {nameof(StubbedDiagnosticEventsGenerator)}.");

            SetPhaseOneCompleted(false);
            var stubbedGenerator = new StubbedDiagnosticEventsGenerator(MaxIterations, PhaseOneIterations);
            Task stubbedGeneratorTask = Task.Run(stubbedGenerator.Run);

            stubbedGenerator.PhaseOneCompletedEvent.Wait();

            Console.WriteLine();
            Console.WriteLine($"Phase one of {nameof(StubbedDiagnosticEventsGenerator)} completed.");
            SetPhaseOneCompleted(true);

            Console.WriteLine($"Starting {nameof(DirectDiagnosticEventsGenerator)}.");

            var directGenerator = new DirectDiagnosticEventsGenerator(MaxIterations, PhaseOneIterations);
            Task directGeneratorTask = Task.Run(directGenerator.Run);

            Task.WaitAll(stubbedGeneratorTask, directGeneratorTask);

            Console.WriteLine();
            Console.WriteLine($"Both, {nameof(StubbedDiagnosticEventsGenerator)} and {nameof(DirectDiagnosticEventsGenerator)} finished.");

            Console.WriteLine();
            Console.WriteLine($"Summary of {nameof(stubbedResultsAccumulator)}:"
                            + $" Received events: {stubbedResultsAccumulator.ReceivedCount}; Proportion: {stubbedResultsAccumulator.ReceivedProportion}.");
            Console.WriteLine(stubbedResultsAccumulator.GetReceivedVisual(ReceivedEventsVisualWidth));

            Console.WriteLine();
            Console.WriteLine($"Summary of {nameof(directResultsAccumulator)}:"
                            + $" Received events: {directResultsAccumulator.ReceivedCount}; Proportion: {directResultsAccumulator.ReceivedProportion}.");
            Console.WriteLine(directResultsAccumulator.GetReceivedVisual(ReceivedEventsVisualWidth));

            Console.WriteLine();
            Console.WriteLine("All done. Press enter to exit.");

            Console.ReadLine();
            Console.WriteLine("Good bye.");
        }

        private void SetPhaseOneCompleted(bool isCompleted)
        {
            if (isCompleted)
            {
                Interlocked.Exchange(ref _isPhaseOneCompleted, 1);
            }
            else
            {
                Interlocked.Exchange(ref _isPhaseOneCompleted, 0);
            }
        }

        private bool GetPhaseOneCompleted()
        {
            int isCompleted = Interlocked.Add(ref _isPhaseOneCompleted, 0);
            return (isCompleted != 0);
        }

        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            if (!GetPhaseOneCompleted())
            {
                Console.WriteLine($"AssemblyResolveEventHandler: Phase One not completed => doing nothing.");
                return null;
            }

            string asmNameRequestedStr = args?.Name;
            if (asmNameRequestedStr == null)
            {
                Console.WriteLine($"AssemblyResolveEventHandler: No good arguments => doing nothing.");
                return null;
            }

            string dsAsmFilePath = Path.Combine(DiagnosticSourceAssemblyHiddenPath, DiagnosticSourceAssemblyFilename);
            AssemblyName asmNameAtPath = GetAssemblyNameFromPath(dsAsmFilePath);
            if (asmNameAtPath == null)
            {
                Console.WriteLine($"AssemblyResolveEventHandler: Cannot extract assembly name from \"{dsAsmFilePath}\". Doing nothing.");
                return null;
            }
            else
            {
                AssemblyName asmNameRequested = new AssemblyName(asmNameRequestedStr);
                if (AreEqual(asmNameAtPath, asmNameRequested))
                {
                    Console.WriteLine($"AssemblyResolveEventHandler: Match. Loading DS from special location.");
                    return Assembly.Load(asmNameAtPath);
                }
                else
                {
                    Console.WriteLine($"AssemblyResolveEventHandler: No Match. Doing nothing.");
                    Console.WriteLine($"    Requested assembly: \"{asmNameRequested.FullName}\";");
                    Console.WriteLine($"    Present assembly:   \"{asmNameAtPath.FullName}\".");
                    return null;
                }
            }
        }

        private static bool AreEqual(AssemblyName asmName1, AssemblyName asmName2)
        {
            if (Object.ReferenceEquals(asmName1, asmName2))
            {
                return true;
            }

            if (asmName1 == null || asmName2 == null)
            {
                return false;
            }

            return AssemblyName.ReferenceMatchesDefinition(asmName1, asmName2) && asmName1.FullName.Equals(asmName2.FullName);
        }

        private static AssemblyName GetAssemblyNameFromPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return AssemblyName.GetAssemblyName(path);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
