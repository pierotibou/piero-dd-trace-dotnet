// <copyright file="TelemetrySettingsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Specialized;
using Datadog.Trace.Configuration;
using Datadog.Trace.Telemetry;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.Telemetry
{
    public class TelemetrySettingsTests
    {
        private readonly ImmutableTracerSettings _tracerSettings;
        private readonly string _defaultAgentUrl;
        private readonly string _defaultIntakeUrl;

        public TelemetrySettingsTests()
        {
            _tracerSettings = new(new TracerSettings());
            _defaultAgentUrl = $"{_tracerSettings.Exporter.AgentUri}{TelemetryConstants.AgentTelemetryEndpoint}";
            _defaultIntakeUrl = "https://instrumentation-telemetry-intake.datadoghq.com/";
        }

        [Theory]
        [InlineData("https://sometest.com", "https://sometest.com/")]
        [InlineData("https://sometest.com/some-path", "https://sometest.com/some-path")]
        [InlineData("https://sometest.com/some-path/", "https://sometest.com/some-path/")]
        public void WhenValidUrlIsProvided_AppendsSlashToPath(string url, string expected)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" },
                { ConfigurationKeys.Telemetry.Uri, url },
                { ConfigurationKeys.ApiKey, "some_key" },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be(expected);
            settings.SendDirectlyToIntake.Should().BeTrue();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Fact]
        public void WhenNoUrlOrApiKeyIsProvided_UsesAgentIntake()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" }
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be(_defaultAgentUrl);
            settings.SendDirectlyToIntake.Should().BeFalse();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Fact]
        public void WhenOnlyApiKeyIsProvided_UsesIntakeUrl()
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" },
                { ConfigurationKeys.ApiKey, "some_key" },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be(_defaultIntakeUrl);
            settings.SendDirectlyToIntake.Should().BeTrue();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Fact]
        public void WhenApiKeyAndDdSiteIsProvided_UsesDdSiteDomain()
        {
            var domain = "my-domain.net";
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" },
                { ConfigurationKeys.ApiKey, "some_key" },
                { ConfigurationKeys.Site, domain },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be($"https://instrumentation-telemetry-intake.{domain}/");
            settings.SendDirectlyToIntake.Should().BeTrue();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Fact]
        public void WhenInvalidUrlApiIsProvided_AndNoApiKey_UsesAgentIntake()
        {
            var url = "https://sometest::";
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" },
                { ConfigurationKeys.Telemetry.Uri, url },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be(_defaultAgentUrl);
            settings.SendDirectlyToIntake.Should().BeFalse();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData("https://sometest::")]
        [InlineData("https://sometest:-1/")]
        [InlineData("some-path/")]
        [InlineData("nada")]
        public void WhenInvalidUrlIsProvided_AndHasApiKey_UsesDefaultIntakeUrl(string url)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, "1" },
                { ConfigurationKeys.Telemetry.Uri, url },
                { ConfigurationKeys.ApiKey, "some_key" },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryUri.Should().Be(_defaultIntakeUrl);
            settings.SendDirectlyToIntake.Should().BeTrue();
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("SOMEKEY", null, true)]
        [InlineData(null, "0", false)]
        [InlineData("SOMEKEY", "0", false)]
        [InlineData(null, "1", true)]
        [InlineData("SOMEKEY", "1", true)]
        public void SetsTelemetryEnabledBasedOnApiKeyAndEnabledSettings(string apiKey, string enabledSetting, bool enabled)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.Enabled, enabledSetting },
                { ConfigurationKeys.ApiKey, apiKey },
            });

            var settings = new TelemetrySettings(source, _tracerSettings);

            settings.TelemetryEnabled.Should().Be(enabled);
            settings.ConfigurationError.Should().BeNullOrEmpty();
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, true)]
        [InlineData(null, false)]
        [InlineData("SOMEKEY", true)]
        [InlineData("SOMEKEY", false)]
        public void SetsAgentlessBasedOnApiKey(string apiKey, bool? agentless)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { ConfigurationKeys.Telemetry.AgentlessEnabled, agentless?.ToString() },
                { ConfigurationKeys.ApiKey, apiKey },
            });
            var hasApiKey = !string.IsNullOrEmpty(apiKey);

            var settings = new TelemetrySettings(source, _tracerSettings);

            if (agentless == true)
            {
                settings.TelemetryEnabled.Should().Be(hasApiKey);
                settings.SendDirectlyToIntake.Should().Be(hasApiKey);

                if (hasApiKey)
                {
                    settings.ConfigurationError.Should().BeNullOrEmpty();
                }
                else
                {
                    settings.ConfigurationError.Should().NotBeNullOrEmpty();
                }
            }
            else if (agentless == false)
            {
                settings.TelemetryEnabled.Should().Be(false);
                settings.SendDirectlyToIntake.Should().Be(false);
                settings.ConfigurationError.Should().BeNullOrEmpty();
            }
            else
            {
                settings.TelemetryEnabled.Should().Be(hasApiKey);
                settings.SendDirectlyToIntake.Should().Be(hasApiKey);
                settings.ConfigurationError.Should().BeNullOrEmpty();
            }
        }
    }
}
