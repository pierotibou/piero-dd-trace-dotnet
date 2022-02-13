// <copyright file="TelemetrySettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Telemetry
{
    internal class TelemetrySettings
    {
        public TelemetrySettings(IConfigurationSource source, ImmutableTracerSettings tracerSettings)
        {
            var apiKey = source?.GetString(ConfigurationKeys.ApiKey);
            var explicitlyEnabled = source?.GetBool(ConfigurationKeys.Telemetry.Enabled);
            var agentlessExplicitlyEnabled = source?.GetBool(ConfigurationKeys.Telemetry.AgentlessEnabled);
            bool agentlessEnabled = false;

            if (agentlessExplicitlyEnabled == true)
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    ConfigurationError = "Telemetry configuration error: Agentless mode was enabled, but no API key was available";
                }
                else
                {
                    agentlessEnabled = true;
                }
            }
            else if (agentlessExplicitlyEnabled is null)
            {
                // if there's an API key, we use agentless mode, otherwise we use the agent
                agentlessEnabled = !string.IsNullOrEmpty(apiKey);
            }

            // disabled by default unless using agentless
            TelemetryEnabled = explicitlyEnabled ?? agentlessEnabled;

            if (TelemetryEnabled && agentlessEnabled)
            {
                // We have an API key, so try to send directly to intake
                ApiKey = apiKey;
                TelemetryEnabled = true;
                SendDirectlyToIntake = true;

                var requestedTelemetryUri = source?.GetString(ConfigurationKeys.Telemetry.Uri);
                if (!string.IsNullOrEmpty(requestedTelemetryUri)
                 && Uri.TryCreate(requestedTelemetryUri, UriKind.Absolute, out var telemetryUri))
                {
                    // telemetry URI provided and well-formed
                    TelemetryUri = telemetryUri;
                }
                else
                {
                    // use the default intake. Use DD_SITE if provided, otherwise use default
                    var siteFromEnv = source.GetString(ConfigurationKeys.Site);
                    var ddSite = string.IsNullOrEmpty(siteFromEnv) ? "datadoghq.com" : siteFromEnv;
                    TelemetryUri = new Uri($"{TelemetryConstants.TelemetryIntakePrefix}.{ddSite}/");
                }
            }
            else if (TelemetryEnabled)
            {
                // no API key provided, so send to the agent instead
                TelemetryUri = new Uri(tracerSettings.Exporter.AgentUri, TelemetryConstants.AgentTelemetryEndpoint);
            }
        }

        /// <summary>
        /// Gets a value indicating whether internal telemetry is enabled
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Telemetry.Enabled"/>
        public bool TelemetryEnabled { get; }

        /// <summary>
        /// Gets a value indicating the URL where telemetry should be sent
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Telemetry.Uri"/>
        public Uri TelemetryUri { get; }

        /// <summary>
        /// Gets the api key to use when sending requests to the telemetry intake
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Gets a value indicating whether telemetry should be sent direct to the intake instead of the agent
        /// </summary>
        public bool SendDirectlyToIntake { get; }

        /// <summary>
        /// Gets a value indicating configuration errors
        /// </summary>
        public string ConfigurationError { get; }

        public static TelemetrySettings FromDefaultSources(ImmutableTracerSettings tracerSettings)
            => new TelemetrySettings(GlobalSettings.CreateDefaultConfigurationSource(), tracerSettings);
    }
}
