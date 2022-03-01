// <copyright file="ApiWebRequestFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;

namespace Datadog.Trace.Agent.Transports
{
    internal class ApiWebRequestFactory : IApiRequestFactory
    {
        private readonly KeyValuePair<string, string>[] _defaultHeaders;
        private WebProxy _proxy;
        private NetworkCredential _credential;

        public ApiWebRequestFactory(KeyValuePair<string, string>[] defaultHeaders)
        {
            _defaultHeaders = defaultHeaders;
        }

        public string Info(Uri endpoint)
        {
            return endpoint.ToString();
        }

        public IApiRequest Create(Uri endpoint)
        {
            var request = WebRequest.CreateHttp(endpoint);
            if (_proxy is not null)
            {
                request.Proxy = _proxy;
            }

            if (_credential is not null)
            {
                request.Credentials = _credential;
            }

            foreach (var pair in _defaultHeaders)
            {
                request.Headers.Add(pair.Key, pair.Value);
            }

            return new ApiWebRequest(request);
        }

        public void SetProxy(WebProxy proxy, NetworkCredential credential)
        {
            _proxy = proxy;
            _credential = credential;
        }
    }
}
