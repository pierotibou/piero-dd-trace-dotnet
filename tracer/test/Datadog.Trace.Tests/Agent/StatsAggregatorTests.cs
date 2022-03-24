// <copyright file="StatsAggregatorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.Agent
{
    public class StatsAggregatorTests
    {
        [Fact]
        public void CallFlushAutomatically()
        {
            var duration = TimeSpan.FromSeconds(1);

            var mutex = new ManualResetEventSlim();

            int invocationCount = 0;

            var api = new Mock<IApi>();
            api.Setup(a => a.SendStatsAsync(It.IsAny<StatsBuffer>(), duration.ToNanoseconds()))
                .Callback(
                    () =>
                    {
                        if (Interlocked.Increment(ref invocationCount) == 2)
                        {
                            mutex.Set();
                        }
                    })
                .Returns(Task.FromResult(true));

            using var aggregator = new StatsAggregator(api.Object, GetSettings(true), duration);

            var stopwatch = Stopwatch.StartNew();

            bool success = false;

            while (stopwatch.Elapsed.Minutes < 1)
            {
                // Flush is not called if no spans are processed
                aggregator.Add(new Span(new SpanContext(1, 1), DateTime.UtcNow));

                if (mutex.Wait(TimeSpan.FromMilliseconds(100)))
                {
                    success = true;
                    break;
                }
            }

            success.Should().BeTrue();
        }

        [Fact]
        public async Task EmptyBuckets()
        {
            var api = new Mock<IApi>();

            using var aggregator = new StatsAggregator(api.Object, GetSettings(true));

            await FluentActions.Invoking(() => aggregator.Flush(new CancellationToken(canceled: true)))
                .Should()
                .ThrowAsync<OperationCanceledException>();

            // No span is pushed so SendStatsAsync shouldn't be called
            api.Verify(a => a.SendStatsAsync(It.IsAny<StatsBuffer>(), It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public void RecordSpans()
        {
            const int millisecondsToNanoseconds = 1_000_000;

            // All spans should be recorded except childSpan
            const long expectedTotalDuration = (100 + 200 + 300 + 500) * millisecondsToNanoseconds;
            const long expectedOkDuration = (100 + 300 + 500) * millisecondsToNanoseconds;
            const long expectedErrorDuration = 200 * millisecondsToNanoseconds;

            using var aggregator = new StatsAggregator(Mock.Of<IApi>(), GetSettings(false));

            var start = DateTimeOffset.UtcNow;

            var simpleSpan = new Span(new SpanContext(1, 1, serviceName: "service"), start);
            simpleSpan.SetDuration(TimeSpan.FromMilliseconds(100));

            var errorSpan = new Span(new SpanContext(2, 2, serviceName: "service"), start);
            errorSpan.Error = true;
            errorSpan.SetDuration(TimeSpan.FromMilliseconds(200));

            var parentSpan = new Span(new SpanContext(3, 3, serviceName: "service"), start);
            parentSpan.SetDuration(TimeSpan.FromMilliseconds(300));

            // childSpan shouldn't be recorded, because it's not top-level and doesn't have the Measured tag
            var childSpan = new Span(new SpanContext(parentSpan.Context, new TraceContext(Mock.Of<IDatadogTracer>()), "service"), start);
            childSpan.SetDuration(TimeSpan.FromMilliseconds(400));

            var measuredChildSpan = new Span(new SpanContext(parentSpan.Context, new TraceContext(Mock.Of<IDatadogTracer>()), "service"), start);
            measuredChildSpan.SetTag(Tags.Measured, "1");
            measuredChildSpan.SetDuration(TimeSpan.FromMilliseconds(500));

            aggregator.Add(simpleSpan, errorSpan, parentSpan, childSpan, measuredChildSpan);

            var buffer = aggregator.CurrentBuffer;

            buffer.Buckets.Should().HaveCount(1);

            var bucket = buffer.Buckets.Values.Single();

            bucket.Duration.Should().Be(expectedTotalDuration);
            bucket.Hits.Should().Be(4);
            bucket.Errors.Should().Be(1);
            bucket.TopLevelHits.Should().Be(3);
            bucket.ErrorSummary.GetCount().Should().Be(1.0);
            bucket.ErrorSummary.GetSum().Should().BeApproximately(
                expectedErrorDuration, expectedErrorDuration * bucket.ErrorSummary.IndexMapping.RelativeAccuracy);
            bucket.OkSummary.GetCount().Should().Be(3.0);
            bucket.OkSummary.GetSum().Should().BeApproximately(
                expectedOkDuration, expectedOkDuration * bucket.OkSummary.IndexMapping.RelativeAccuracy);
        }

        private static ImmutableTracerSettings GetSettings(bool enableStats)
            => new TracerSettings { TracerStatsEnabled = enableStats }.Build();
    }
}