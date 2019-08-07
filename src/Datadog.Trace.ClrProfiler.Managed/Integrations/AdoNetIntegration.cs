using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// AdoNetIntegration provides methods that add tracing to ADO.NET calls.
    /// </summary>
    public static class AdoNetIntegration
    {
        private const string IntegrationName = "AdoNet";
        private const string Major4 = "4";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(AdoNetIntegration));

        /// <summary>
        /// Wrapper method that instruments <see cref="System.Data.Common.DbCommand.ExecuteDbDataReader"/>.
        /// </summary>
        /// <param name="this">The <see cref="DbCommand"/> that is references by the "this" pointer in the instrumented method.</param>
        /// <param name="behavior">A value from <see cref="CommandBehavior"/>.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssembly = "System.Data", // .NET Framework
            TargetType = "System.Data.Common.DbCommand",
            TargetSignatureTypes = new[] { "System.Data.Common.DbDataReader", "System.Data.CommandBehavior" },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        [InterceptMethod(
            TargetAssembly = "System.Data.Common", // .NET Core
            TargetType = "System.Data.Common.DbCommand",
            TargetSignatureTypes = new[] { "System.Data.Common.DbDataReader", "System.Data.CommandBehavior" },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteDbDataReader(object @this, int behavior, int opCode, int mdToken)
        {
            var command = (DbCommand)@this;
            var commandBehavior = (CommandBehavior)behavior;

            var executeReader = Emit.DynamicMethodBuilder<Func<object, CommandBehavior, object>>
                                    .GetOrCreateMethodCallDelegate(
                                         typeof(DbCommand),
                                         "ExecuteDbDataReader",
                                         (OpCodeValue)opCode);

            using (var scope = CreateScope(command))
            {
                try
                {
                    return executeReader(command, commandBehavior);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrapper method that instruments <see cref="System.Data.Common.DbCommand.ExecuteDbDataReader"/>.
        /// </summary>
        /// <param name="this">The <see cref="DbCommand"/> that is references by the "this" pointer in the instrumented method.</param>
        /// <param name="behavior">A value from <see cref="CommandBehavior"/>.</param>
        /// <param name="cancellationTokenSource">A cancellation token source that can be used to cancel the async operation.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssembly = "System.Data", // .NET Framework
            TargetType = "System.Data.Common.DbCommand",
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Data.Common.DbDataReader>", "System.Data.CommandBehavior", "System.Threading.CancellationToken" },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        [InterceptMethod(
            TargetAssembly = "System.Data.Common", // .NET Core
            TargetType = "System.Data.Common.DbCommand",
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Data.Common.DbDataReader>", "System.Data.CommandBehavior", "System.Threading.CancellationToken" },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteDbDataReaderAsync(object @this, int behavior, object cancellationTokenSource, int opCode, int mdToken)
        {
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            var command = (DbCommand)@this;
            var commandBehavior = (CommandBehavior)behavior;
            var callOpCode = (OpCodeValue)opCode;

            return ExecuteDbDataReaderAsyncInternal(command, commandBehavior, cancellationToken, callOpCode);
        }

        private static async Task<object> ExecuteDbDataReaderAsyncInternal(
            DbCommand command,
            CommandBehavior behavior,
            CancellationToken cancellationToken,
            OpCodeValue callOpCode)
        {
            var executeReader = Emit.DynamicMethodBuilder<Func<object, CommandBehavior, CancellationToken, Task<object>>>
                                    .GetOrCreateMethodCallDelegate(
                                         typeof(DbCommand),
                                         "ExecuteDbDataReaderAsync",
                                         callOpCode);

            using (var scope = CreateScope(command))
            {
                try
                {
                    return await executeReader(command, behavior, cancellationToken);
                }
                catch (Exception ex) when (scope?.Span.SetExceptionForFilter(ex) ?? false)
                {
                    // unreachable code
                    throw;
                }
            }
        }

        private static Scope CreateScope(DbCommand command)
        {
            Scope scope = null;

            try
            {
                Tracer tracer = Tracer.Instance;

                if (!tracer.Settings.IsIntegrationEnabled(IntegrationName))
                {
                    // integration disabled, don't create a scope, skip this trace
                    return null;
                }

                string dbType = GetDbType(command.GetType().Name);

                if (dbType == null)
                {
                    // don't create a scope, skip this trace
                    return null;
                }

                string serviceName = $"{tracer.DefaultServiceName}-{dbType}";
                string operationName = $"{dbType}.query";

                scope = tracer.StartActive(operationName, serviceName: serviceName);
                var span = scope.Span;
                span.SetTag(Tags.DbType, dbType);
                span.AddTagsFromDbCommand(command);

                // set analytics sample rate if enabled
                var analyticsSampleRate = tracer.Settings.GetIntegrationAnalyticsSampleRate(IntegrationName, enabledWithGlobalSetting: false);
                span.SetMetric(Tags.Analytics, analyticsSampleRate);
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error creating or populating scope.", ex);
            }

            return scope;
        }

        private static string GetDbType(string commandTypeName)
        {
            switch (commandTypeName)
            {
                case "SqlCommand":
                    return "sql-server";
                case "NpgsqlCommand":
                    return "postgres";
                case "InterceptableDbCommand":
                case "ProfiledDbCommand":
                    // don't create spans for these
                    return null;
                default:
                    const string commandSuffix = "Command";

                    // remove "Command" suffix if present
                    return commandTypeName.EndsWith(commandSuffix)
                               ? commandTypeName.Substring(0, commandTypeName.Length - commandSuffix.Length).ToLowerInvariant()
                               : commandTypeName.ToLowerInvariant();
            }
        }
    }
}
