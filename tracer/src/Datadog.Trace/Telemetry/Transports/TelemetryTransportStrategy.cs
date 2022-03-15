// <copyright file="TelemetryTransportStrategy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.StreamFactories;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Configuration;
using Datadog.Trace.HttpOverStreams;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Telemetry.Transports;

internal static class TelemetryTransportStrategy
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Tracer>();
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static IApiRequestFactory GetDirectIntakeFactory(ImmutableExporterSettings settings, string apiKey)
    {
#if NETCOREAPP
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(HttpClientRequestFactory));
        return new HttpClientRequestFactory(TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#else
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(ApiWebRequestFactory));
        return new ApiWebRequestFactory(TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#endif
    }

    public static IApiRequestFactory GetAgentIntakeFactory(ImmutableExporterSettings settings)
    {
        // use the same transport for telemetry as we do for traces
        var strategy = settings.TracesTransport;

        switch (strategy)
        {
            case TracesTransportType.CustomTcpProvider:
                Log.Information("Using {FactoryType} for telemetry transport to agent.", nameof(TcpStreamFactory));
                return new HttpStreamRequestFactory(new TcpStreamFactory(settings.AgentUri.Host, settings.AgentUri.Port), DatadogHttpClient.CreateTelemetryAgentClient());
            case TracesTransportType.WindowsNamedPipe:
                Log.Information<string, string, int>("Using {FactoryType} for telemetry transport, with pipe name {PipeName} and timeout {Timeout}ms.", nameof(NamedPipeClientStreamFactory), settings.TracesPipeName, settings.TracesPipeTimeoutMs);
                return new HttpStreamRequestFactory(new NamedPipeClientStreamFactory(settings.TracesPipeName, settings.TracesPipeTimeoutMs), DatadogHttpClient.CreateTelemetryAgentClient());
            case TracesTransportType.UnixDomainSocket:
#if NETCOREAPP3_1_OR_GREATER
                Log.Information<string, string, int>("Using {FactoryType} for telemetry transport, with Unix Domain Sockets path {Path} and timeout {Timeout}ms.", nameof(UnixDomainSocketStreamFactory), settings.TracesUnixDomainSocketPath, settings.TracesPipeTimeoutMs);
                return new HttpStreamRequestFactory(new UnixDomainSocketStreamFactory(settings.TracesUnixDomainSocketPath), DatadogHttpClient.CreateTelemetryAgentClient());
#else
                Log.Error("Using Unix Domain Sockets for telemetry transport is only supported on .NET Core 3.1 and greater. Falling back to default transport.");
                goto case TracesTransportType.Default;
#endif
            case TracesTransportType.Default:
            default:
#if NETCOREAPP
                Log.Information("Using {FactoryType} for telemetry transport.", nameof(HttpClientRequestFactory));
                return new HttpClientRequestFactory(TelemetryHttpHeaderNames.GetDefaultAgentHeaders(), timeout: Timeout);
#else
                Log.Information("Using {FactoryType} for telemetry transport.", nameof(ApiWebRequestFactory));
                return new ApiWebRequestFactory(TelemetryHttpHeaderNames.GetDefaultAgentHeaders(), timeout: Timeout);
#endif
        }
    }
}
