// <copyright file="AspNetCore5.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_0_OR_GREATER

using System;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class AspNetCore5 : AspNetCoreBase, IDisposable
    {
        public AspNetCore5(ITestOutputHelper outputHelper)
            : base("AspNetCore5", outputHelper, "/shutdown")
        {
        }
    }
}
#endif
