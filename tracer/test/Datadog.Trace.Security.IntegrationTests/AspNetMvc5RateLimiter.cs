// <copyright file="AspNetMvc5RateLimiter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NET461
using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

namespace Datadog.Trace.Security.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetMvc5RateLimiterIntegratedWithSecurity : AspNetMvc5RateLimiter
    {
        public AspNetMvc5RateLimiterIntegratedWithSecurity(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, classicMode: false, enableSecurity: true, blockingEnabled: true)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetMvc5RateLimiterIntegratedWithoutSecurity : AspNetMvc5RateLimiter
    {
        public AspNetMvc5RateLimiterIntegratedWithoutSecurity(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, classicMode: false, enableSecurity: false, blockingEnabled: false)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetMvc5RateLimiterClassicWithSecurity : AspNetMvc5RateLimiter
    {
        public AspNetMvc5RateLimiterClassicWithSecurity(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, classicMode: true, enableSecurity: true, blockingEnabled: true)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetMvc5RateLimiterClassicWithoutSecurity : AspNetMvc5RateLimiter
    {
        public AspNetMvc5RateLimiterClassicWithoutSecurity(IisFixture iisFixture, ITestOutputHelper output)
            : base(iisFixture, output, classicMode: true, enableSecurity: false, blockingEnabled: false)
        {
        }
    }

    public abstract class AspNetMvc5RateLimiter : AspNetBase, IClassFixture<IisFixture>
    {
        private const int _traceRateLimit = 30;
        private readonly IisFixture _iisFixture;
        private readonly bool _enableSecurity;
        private readonly bool _blockingEnabled;

        public AspNetMvc5RateLimiter(IisFixture iisFixture, ITestOutputHelper output, bool classicMode, bool enableSecurity, bool blockingEnabled)
            : base(nameof(AspNetMvc5), output, "/home/shutdown", @"test\test-applications\security\aspnet")
        {
            SetSecurity(enableSecurity);
            SetAppSecBlockingEnabled(blockingEnabled);
            _iisFixture = iisFixture;
            _enableSecurity = enableSecurity;
            _blockingEnabled = blockingEnabled;
            SetEnvironmentVariable(ConfigurationKeys.AppSecTraceRateLimit, _traceRateLimit.ToString());

            _iisFixture.TryStartIis(this, classicMode ? IisAppType.AspNetClassic : IisAppType.AspNetIntegrated);
            SetHttpPort(iisFixture.HttpPort);
        }

        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("LoadFromGAC", "True")]
        [Theory]
        [InlineData]
        public async Task TestRateLimiterSecurity(string url = DefaultAttackUrl)
        {
            var totalRequests = 80;
            // tracing module and mvc actions
            await TestRateLimiter(_enableSecurity, url, _iisFixture.Agent, _traceRateLimit, totalRequests, totalRequests * 2);
        }
    }
}
#endif