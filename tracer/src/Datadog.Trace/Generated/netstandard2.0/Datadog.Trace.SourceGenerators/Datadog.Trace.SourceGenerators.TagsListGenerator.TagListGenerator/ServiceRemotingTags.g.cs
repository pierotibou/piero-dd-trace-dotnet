﻿// <auto-generated/>
#nullable enable

using Datadog.Trace.Processors;

namespace Datadog.Trace.ServiceFabric
{
    partial class ServiceRemotingTags
    {
        private static readonly byte[] SpanKindBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("span.kind");
        private static readonly byte[] ApplicationIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.application-id");
        private static readonly byte[] ApplicationNameBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.application-name");
        private static readonly byte[] PartitionIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.partition-id");
        private static readonly byte[] NodeIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.node-id");
        private static readonly byte[] NodeNameBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.node-name");
        private static readonly byte[] ServiceNameBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-name");
        private static readonly byte[] RemotingUriBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-remoting.uri");
        private static readonly byte[] RemotingMethodNameBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-remoting.method-name");
        private static readonly byte[] RemotingMethodIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-remoting.method-id");
        private static readonly byte[] RemotingInterfaceIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-remoting.interface-id");
        private static readonly byte[] RemotingInvocationIdBytes = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("service-fabric.service-remoting.invocation-id");

        public override string? GetTag(string key)
        {
            return key switch
            {
                "span.kind" => SpanKind,
                "service-fabric.application-id" => ApplicationId,
                "service-fabric.application-name" => ApplicationName,
                "service-fabric.partition-id" => PartitionId,
                "service-fabric.node-id" => NodeId,
                "service-fabric.node-name" => NodeName,
                "service-fabric.service-name" => ServiceName,
                "service-fabric.service-remoting.uri" => RemotingUri,
                "service-fabric.service-remoting.method-name" => RemotingMethodName,
                "service-fabric.service-remoting.method-id" => RemotingMethodId,
                "service-fabric.service-remoting.interface-id" => RemotingInterfaceId,
                "service-fabric.service-remoting.invocation-id" => RemotingInvocationId,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "service-fabric.application-id": 
                    ApplicationId = value;
                    break;
                case "service-fabric.application-name": 
                    ApplicationName = value;
                    break;
                case "service-fabric.partition-id": 
                    PartitionId = value;
                    break;
                case "service-fabric.node-id": 
                    NodeId = value;
                    break;
                case "service-fabric.node-name": 
                    NodeName = value;
                    break;
                case "service-fabric.service-name": 
                    ServiceName = value;
                    break;
                case "service-fabric.service-remoting.uri": 
                    RemotingUri = value;
                    break;
                case "service-fabric.service-remoting.method-name": 
                    RemotingMethodName = value;
                    break;
                case "service-fabric.service-remoting.method-id": 
                    RemotingMethodId = value;
                    break;
                case "service-fabric.service-remoting.interface-id": 
                    RemotingInterfaceId = value;
                    break;
                case "service-fabric.service-remoting.invocation-id": 
                    RemotingInvocationId = value;
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        protected override int WriteAdditionalTags(ref byte[] bytes, ref int offset, ITagProcessor[] tagProcessors)
        {
            var count = 0;
            if (SpanKind != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, SpanKindBytes, SpanKind, tagProcessors);
            }

            if (ApplicationId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, ApplicationIdBytes, ApplicationId, tagProcessors);
            }

            if (ApplicationName != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, ApplicationNameBytes, ApplicationName, tagProcessors);
            }

            if (PartitionId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, PartitionIdBytes, PartitionId, tagProcessors);
            }

            if (NodeId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, NodeIdBytes, NodeId, tagProcessors);
            }

            if (NodeName != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, NodeNameBytes, NodeName, tagProcessors);
            }

            if (ServiceName != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, ServiceNameBytes, ServiceName, tagProcessors);
            }

            if (RemotingUri != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, RemotingUriBytes, RemotingUri, tagProcessors);
            }

            if (RemotingMethodName != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, RemotingMethodNameBytes, RemotingMethodName, tagProcessors);
            }

            if (RemotingMethodId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, RemotingMethodIdBytes, RemotingMethodId, tagProcessors);
            }

            if (RemotingInterfaceId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, RemotingInterfaceIdBytes, RemotingInterfaceId, tagProcessors);
            }

            if (RemotingInvocationId != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, RemotingInvocationIdBytes, RemotingInvocationId, tagProcessors);
            }

            return count + base.WriteAdditionalTags(ref bytes, ref offset, tagProcessors);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (SpanKind != null)
            {
                sb.Append("span.kind (tag):")
                  .Append(SpanKind)
                  .Append(',');
            }

            if (ApplicationId != null)
            {
                sb.Append("service-fabric.application-id (tag):")
                  .Append(ApplicationId)
                  .Append(',');
            }

            if (ApplicationName != null)
            {
                sb.Append("service-fabric.application-name (tag):")
                  .Append(ApplicationName)
                  .Append(',');
            }

            if (PartitionId != null)
            {
                sb.Append("service-fabric.partition-id (tag):")
                  .Append(PartitionId)
                  .Append(',');
            }

            if (NodeId != null)
            {
                sb.Append("service-fabric.node-id (tag):")
                  .Append(NodeId)
                  .Append(',');
            }

            if (NodeName != null)
            {
                sb.Append("service-fabric.node-name (tag):")
                  .Append(NodeName)
                  .Append(',');
            }

            if (ServiceName != null)
            {
                sb.Append("service-fabric.service-name (tag):")
                  .Append(ServiceName)
                  .Append(',');
            }

            if (RemotingUri != null)
            {
                sb.Append("service-fabric.service-remoting.uri (tag):")
                  .Append(RemotingUri)
                  .Append(',');
            }

            if (RemotingMethodName != null)
            {
                sb.Append("service-fabric.service-remoting.method-name (tag):")
                  .Append(RemotingMethodName)
                  .Append(',');
            }

            if (RemotingMethodId != null)
            {
                sb.Append("service-fabric.service-remoting.method-id (tag):")
                  .Append(RemotingMethodId)
                  .Append(',');
            }

            if (RemotingInterfaceId != null)
            {
                sb.Append("service-fabric.service-remoting.interface-id (tag):")
                  .Append(RemotingInterfaceId)
                  .Append(',');
            }

            if (RemotingInvocationId != null)
            {
                sb.Append("service-fabric.service-remoting.invocation-id (tag):")
                  .Append(RemotingInvocationId)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}
