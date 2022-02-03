// <copyright file="IHttpResponse.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK
#nullable enable

using System;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNetCore
{
    internal interface IHttpResponse
    {
        IHeaderDictionary Headers { get; }

        int StatusCode { get; }

        void OnCompleted(Func<Task> callback);

        void OnStarting(Func<Task> callback);

        void RegisterForDispose(IDisposable disposable);
    }
}
#endif
