// <copyright file="IApiRequestFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Net;

namespace Datadog.Trace.Agent
{
    internal interface IApiRequestFactory
    {
        string Info(Uri endpoint);

        IApiRequest Create(Uri endpoint);

        void SetProxy(WebProxy proxy, NetworkCredential credential);
    }
}
