// <copyright file="StatsBufferTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Datadog.Trace.Agent;
using FluentAssertions;
using MessagePack;
using Xunit;

namespace Datadog.Trace.Tests.Agent
{
    public class StatsBufferTests
    {
        [Fact]
        public void KeyEquality()
        {
            var key1 = new StatsAggregationKey("resource1", "service1", "operation1", "type1", 1);
            var key2 = new StatsAggregationKey("resource1", "service1", "operation1", "type1", 1);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Serialization()
        {
            const long expectedDuration = 42;

            var payload = new ClientStatsPayload { Environment = "Env", HostName = "Hostname", ServiceName = "Service", Version = "v99.99" };

            var buffer = new StatsBuffer(payload);

            var key1 = new StatsAggregationKey("resource1", "service1", "operation1", "type1", 1);
            var key2 = new StatsAggregationKey("resource2", "service2", "operation2", "type2", 2);
            var key3 = new StatsAggregationKey("resource3", "service3", "operation3", "type3", 2);

            var statsBucket1 = new StatsBucket(key1) { Duration = 1, Errors = 11, Hits = 111, TopLevelHits = 10 };
            var statsBucket2 = new StatsBucket(key2) { Duration = 2, Errors = 22, Hits = 222, TopLevelHits = 20 };
            var statsBucket3 = new StatsBucket(key3) { Duration = 3, Errors = 0, Hits = 0, TopLevelHits = 0 };

            buffer.Buckets.Add(key1, statsBucket1);
            buffer.Buckets.Add(key2, statsBucket2);
            buffer.Buckets.Add(key3, statsBucket3);

            var stream = new MemoryStream();
            buffer.Serialize(stream, expectedDuration);
            var result = MessagePackSerializer.Deserialize<MockClientStatsPayload>(stream.ToArray());

            result.Hostname.Should().Be(payload.HostName);
            result.Env.Should().Be(payload.Environment);
            result.Version.Should().Be(payload.Version);
            result.Lang.Should().Be(TracerConstants.Language);
            result.TracerVersion.Should().Be(TracerConstants.AssemblyVersion);
            result.RuntimeId.Should().Be(Tracer.RuntimeId);
            result.Sequence.Should().Be(1);
            result.AgentAggregation.Should().BeNull();
            result.Service.Should().Be(payload.ServiceName);
            result.Stats.Should().HaveCount(1);

            var bucket = result.Stats[0];

            bucket.Start.Should().Be(buffer.Start);
            bucket.Duration.Should().Be(expectedDuration);
            bucket.AgentTimeShift.Should().Be(0);
            bucket.Stats.Should().HaveCount(2); // statsBucket3 isn't serialized because it has no hits

            AssertStatsGroup(bucket.Stats.Single(g => g.Name == key1.OperationName), key1, statsBucket1);
            AssertStatsGroup(bucket.Stats.Single(g => g.Name == key2.OperationName), key2, statsBucket2);
        }

        [Fact]
        public void Reset()
        {
            var buffer = new StatsBuffer(new ClientStatsPayload());

            var key1 = new StatsAggregationKey("resource1", "service1", "operation1", "type1", 1);
            var key2 = new StatsAggregationKey("resource2", "service2", "operation2", "type2", 2);

            var statsBucket1 = new StatsBucket(key1) { Duration = 1, Errors = 11, Hits = 111, TopLevelHits = 10 };
            var statsBucket2 = new StatsBucket(key2) { Duration = 2, Errors = 0, Hits = 0, TopLevelHits = 0 };

            buffer.Buckets.Add(key1, statsBucket1);
            buffer.Buckets.Add(key2, statsBucket2);

            // First reset - key1 should be cleared, key2 should be removed (because it has no hits)
            buffer.Reset();

            buffer.Buckets.Should().HaveCount(1);

            var kvp = buffer.Buckets.Single();

            kvp.Key.Should().Be(key1);

            kvp.Value.Duration.Should().Be(0);
            kvp.Value.OkSummary.GetCount().Should().Be(0);
            kvp.Value.ErrorSummary.GetCount().Should().Be(0);
            kvp.Value.Errors.Should().Be(0);
            kvp.Value.Hits.Should().Be(0);
            kvp.Value.TopLevelHits.Should().Be(0);

            // Second reset - key1 should be removed
            buffer.Reset();

            buffer.Buckets.Should().BeEmpty();
        }

        private static void AssertStatsGroup(MockClientGroupedStats group, StatsAggregationKey expectedKey, StatsBucket expectedBucket)
        {
            group.Service.Should().Be(expectedKey.Service);
            group.Name.Should().Be(expectedKey.OperationName);
            group.Resource.Should().Be(expectedKey.Resource);
            group.HttpStatusCode.Should().Be(expectedKey.HttpStatusCode);
            group.Type.Should().Be(expectedKey.Type);
            group.DbType.Should().BeNull();
            group.Hits.Should().Be(expectedBucket.Hits);
            group.Errors.Should().Be(expectedBucket.Errors);
            group.Duration.Should().Be(expectedBucket.Duration);
            group.Synthetics.Should().BeFalse();
            group.TopLevelhits.Should().Be(expectedBucket.TopLevelHits);

            var stream = new MemoryStream();
            expectedBucket.ErrorSummary.Serialize(stream);
            group.ErrorSummary.Should().Equal(stream.ToArray());

            stream = new MemoryStream();
            expectedBucket.OkSummary.Serialize(stream);
            group.OkSummary.Should().Equal(stream.ToArray());
        }

        [MessagePackObject]
        public class MockClientStatsPayload
        {
            [Key("hostname")]
            public string Hostname { get; set; }

            [Key("env")]
            public string Env { get; set; }

            [Key("version")]
            public string Version { get; set; }

            [Key("lang")]
            public string Lang { get; set; }

            [Key("tracerVersion")]
            public string TracerVersion { get; set; }

            [Key("runtimeID")]
            public string RuntimeId { get; set; }

            [Key("sequence")]
            public long Sequence { get; set; }

            [Key("agentAggregation")]
            public string AgentAggregation { get; set; }

            [Key("service")]
            public string Service { get; set; }

            [Key("stats")]
            public List<MockClientStatsBucket> Stats { get; set; }
        }

        [MessagePackObject]
        public class MockClientStatsBucket
        {
            [Key("start")]
            public long Start { get; set; }

            [Key("duration")]
            public long Duration { get; set; }

            [Key("stats")]
            public List<MockClientGroupedStats> Stats { get; set; }

            [Key("agentTimeShift")]
            public long AgentTimeShift { get; set; }
        }

        [MessagePackObject]
        public class MockClientGroupedStats
        {
            [Key("service")]
            public string Service { get; set; }

            [Key("name")]
            public string Name { get; set; }

            [Key("resource")]
            public string Resource { get; set; }

            [Key("HTTP_status_code")]
            public int HttpStatusCode { get; set; }

            [Key("type")]
            public string Type { get; set; }

            [Key("DB_type")]
            public string DbType { get; set; }

            [Key("hits")]
            public long Hits { get; set; }

            [Key("errors")]
            public long Errors { get; set; }

            [Key("duration")]
            public long Duration { get; set; }

            [Key("okSummary")]
            public byte[] OkSummary { get; set; }

            [Key("errorSummary")]
            public byte[] ErrorSummary { get; set; }

            [Key("synthetics")]
            public bool Synthetics { get; set; }

            [Key("topLevelHits")]
            public long TopLevelhits { get; set; }
        }
    }
}
