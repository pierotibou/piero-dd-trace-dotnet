// <copyright file="UnixDomainSocketConfig.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.TestHelpers
{
    public class UnixDomainSocketConfig
    {
        public UnixDomainSocketConfig(string traces)
        {
            Traces = traces;
        }

        public UnixDomainSocketConfig(string traces, string metrics, bool useDogstatsD = false)
        {
            Traces = traces;
            Metrics = metrics;
            UseDogstatsD = useDogstatsD;
        }

        public string Traces { get; }

        public string Metrics { get; }

        public bool UseDogstatsD { get; }
    }
}
