// <copyright file="GetAssemblyTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Datadog.Trace.DuckTyping.Tests
{
    [Collection(nameof(GetAssemblyTestsCollection))]
    public class GetAssemblyTests
    {
        [Fact]
        public void GetAssemblyTest()
        {
            var lstExceptions = new List<Exception>();
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    lstExceptions.AddRange(ex.LoaderExceptions);
                }
            }

            if (lstExceptions.Count > 0)
            {
                throw new AggregateException(lstExceptions.ToArray());
            }
        }
    }
}
