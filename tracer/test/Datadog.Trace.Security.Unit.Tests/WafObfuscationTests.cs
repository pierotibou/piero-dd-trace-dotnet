// <copyright file="WafObfuscationTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.Waf;
using Datadog.Trace.AppSec.Waf.ReturnTypes.Managed;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Security.Unit.Tests
{
    [Collection("WafTests")]
    public class WafObfuscationTests
    {
        [Theory]
        [InlineData(false, "bearer", "this is a very secret value having the select pg_sleep attack", "select pg_sleep")]
        [InlineData(false, "password", "select pg_sleep", "select pg_sleep")]
        [InlineData(false, "pwd", "select pg_sleep", "select pg_sleep")]
        [InlineData(true, "bearer", "this is a very secret value having the select pg_sleep attack", "select pg_sleep")]
        [InlineData(true, "password", "select pg_sleep", "select pg_sleep")]
        [InlineData(true, "pwd", "select pg_sleep", "select pg_sleep")]
        public void AttacksWithSecrets(bool obfuscate, string key, string fullAttack, string highlight)
        {
            var value = new Dictionary<string, string[]>
                {
                    {
                        key, new string[]
                        {
                            fullAttack
                        }
                    }
                };
            var args = new Dictionary<string, object>
            {
                {
                    AddressesConstants.RequestQuery, value
                }
            };
            if (!args.ContainsKey(AddressesConstants.RequestUriRaw))
            {
                args.Add(AddressesConstants.RequestUriRaw, "http://localhost:54587/");
            }

            if (!args.ContainsKey(AddressesConstants.RequestMethod))
            {
                args.Add(AddressesConstants.RequestMethod, "GET");
            }

            using var waf =
                    obfuscate
                        ? Waf.Create(SecuritySettings.ObfuscationParameterKeyRegexDefault, SecuritySettings.ObfuscationParameterValueRegexDefault)
                        : Waf.Create(string.Empty, string.Empty);

            var expectedHighlight = obfuscate ? "<Redacted>" : highlight;
            var expectedValue = obfuscate ? "<Redacted>" : fullAttack;

            waf.Should().NotBeNull();
            using var context = waf.CreateContext();
            context.AggregateAddresses(args);
            var result = context.Run(1_000_000);
            result.ReturnCode.Should().Be(ReturnCode.Monitor);
            var resultData = JsonConvert.DeserializeObject<WafMatch[]>(result.Data).FirstOrDefault();
            resultData.RuleMatches[0].Parameters[0].Address.Should().Be(AddressesConstants.RequestQuery);
            resultData.RuleMatches[0].Parameters[0].Highlight[0].Should().Be(expectedHighlight);
            resultData.RuleMatches[0].Parameters[0].Value.Should().Be(expectedValue);
        }
    }
}