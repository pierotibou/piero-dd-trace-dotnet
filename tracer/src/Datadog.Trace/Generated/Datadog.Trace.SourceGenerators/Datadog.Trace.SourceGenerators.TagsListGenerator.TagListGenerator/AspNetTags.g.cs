﻿// <auto-generated/>
#nullable enable

namespace Datadog.Trace.Tagging
{
    partial class AspNetTags
    {
        private static readonly byte[] _bytesAspNetRoute = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("aspnet.route");
        private static readonly byte[] _bytesAspNetController = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("aspnet.controller");
        private static readonly byte[] _bytesAspNetAction = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("aspnet.action");
        private static readonly byte[] _bytesAspNetArea = Datadog.Trace.Vendors.MessagePack.StringEncoding.UTF8.GetBytes("aspnet.area");

        public override string? GetTag(string key)
        {
            return key switch
            {
                "aspnet.route" => AspNetRoute,
                "aspnet.controller" => AspNetController,
                "aspnet.action" => AspNetAction,
                "aspnet.area" => AspNetArea,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "aspnet.route": 
                    AspNetRoute = value;
                    break;
                case "aspnet.controller": 
                    AspNetController = value;
                    break;
                case "aspnet.action": 
                    AspNetAction = value;
                    break;
                case "aspnet.area": 
                    AspNetArea = value;
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        protected override int WriteAdditionalTags(ref byte[] bytes, ref int offset)
        {
            var count = 0;
            if (AspNetRoute != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, _bytesAspNetRoute, AspNetRoute);
            }

            if (AspNetController != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, _bytesAspNetController, AspNetController);
            }

            if (AspNetAction != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, _bytesAspNetAction, AspNetAction);
            }

            if (AspNetArea != null)
            {
                count++;
                WriteTag(ref bytes, ref offset, _bytesAspNetArea, AspNetArea);
            }

            return count + base.WriteAdditionalTags(ref bytes, ref offset);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (AspNetRoute != null)
            {
                sb.Append("aspnet.route (tag):")
                  .Append(AspNetRoute)
                  .Append(',');
            }

            if (AspNetController != null)
            {
                sb.Append("aspnet.controller (tag):")
                  .Append(AspNetController)
                  .Append(',');
            }

            if (AspNetAction != null)
            {
                sb.Append("aspnet.action (tag):")
                  .Append(AspNetAction)
                  .Append(',');
            }

            if (AspNetArea != null)
            {
                sb.Append("aspnet.area (tag):")
                  .Append(AspNetArea)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}