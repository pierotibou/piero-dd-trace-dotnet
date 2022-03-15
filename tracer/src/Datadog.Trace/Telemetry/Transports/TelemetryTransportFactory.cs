// <copyright file="TelemetryTransportFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Telemetry.Transports
{
    internal class TelemetryTransportFactory
    {
        public static ITelemetryTransport Create(
            ImmutableExporterSettings exportSettings,
            bool sendDirectlyToIntake,
            Uri telemetryUri,
            string apiKey)
        {
            var apiRequestFactory = sendDirectlyToIntake
                                        ? TelemetryTransportStrategy.GetDirectIntakeFactory(exportSettings, apiKey)
                                        : TelemetryTransportStrategy.GetAgentIntakeFactory(exportSettings);

            return new JsonTelemetryTransport(apiRequestFactory, telemetryUri);
        }
    }
}
