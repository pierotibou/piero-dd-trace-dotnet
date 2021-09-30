// <copyright file="RegexMatchCondition.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.AppSec.DataFormat;
using Datadog.Trace.Logging;
using engine;

namespace Datadog.Trace.AppSec.Waf.Rules
{
    internal class RegexMatchCondition : ICondition
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(RegexMatchCondition));

        private readonly List<string> inputs;
        private readonly RE2 pattern;

        public RegexMatchCondition(List<string> inputs, string pattern)
        {
            this.inputs = inputs;
            this.pattern = RE2.compile(pattern);
        }

        public bool IsMatch(Dictionary<string, object> data)
        {
            return IsMatchInternal(data);
        }

        private bool IsMatchInternal(Dictionary<string, object> data, string transformKey = null)
        {
            foreach (var input in inputs)
            {
                var key = transformKey == null ? input : RuleUtils.MakeTransformInputKey(transformKey, input);
                if (data.TryGetValue(key, out var currentValue))
                {
                    return Visitor.DepthFirstSearch(data, stringNodeValue => pattern.match(stringNodeValue));
                }
            }

            return false;
        }

        public bool IsTransformedMatch(Dictionary<string, object> data, string transformation)
        {
            return IsMatchInternal(data, transformation);
        }
    }
}
