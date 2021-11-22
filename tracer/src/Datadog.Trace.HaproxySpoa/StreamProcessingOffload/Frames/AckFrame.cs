// <copyright file="AckFrame.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="AckFrame.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using HAProxy.StreamProcessingOffload.Agent.Payloads;

namespace HAProxy.StreamProcessingOffload.Agent.Frames
{
    internal class AckFrame : Frame
    {
        public AckFrame(long streamId, long frameId, IList<SpoeAction> actions)
            : base(FrameType.Ack)
        {
            this.Metadata.Flags.Fin = true;
            this.Metadata.Flags.Abort = false;
            this.Metadata.StreamId = VariableInt.EncodeVariableInt(streamId);
            this.Metadata.FrameId = VariableInt.EncodeVariableInt(frameId);
            this.Payload = new ListOfActionsPayload()
            {
                Actions = actions
            };
        }

        public List<Frame> FragmentFrame(uint maxFrameSize)
        {
            var frames = new List<Frame>();

            if (this.Bytes.Length <= maxFrameSize)
            {
                frames.Add(this);
                return frames;
            }

            int payloadBytesTaken = 0;
            int offset = 0;

            // truncated ACK frame
            var ackFrame = new AckFrame(0, 0, new List<SpoeAction>());
            ackFrame.Metadata = this.Metadata;
            ackFrame.Metadata.Flags.Fin = false;
            ackFrame.Payload = new RawDataPayload();
            ackFrame.Payload.Parse(this.Payload.Bytes.Take((int)maxFrameSize - ackFrame.Metadata.Bytes.Length - 5).ToArray(), ref offset); // subtract 5 for length and type
            payloadBytesTaken += ((int)maxFrameSize - ackFrame.Metadata.Bytes.Length - 5);
            frames.Add(ackFrame);

            while (payloadBytesTaken < this.Payload.Bytes.Length)
            {
                var unsetFrame = new UnsetFrame(this.Metadata.StreamId.Value, this.Metadata.FrameId.Value, false, false);
                offset = 0;
                unsetFrame.Payload.Parse(this.Payload.Bytes.Skip(payloadBytesTaken).Take((int)maxFrameSize - unsetFrame.Metadata.Bytes.Length - 5).ToArray(), ref offset);
                payloadBytesTaken += ((int)maxFrameSize - unsetFrame.Metadata.Bytes.Length - 5);
                frames.Add(unsetFrame);
            }

            frames.Last().Metadata.Flags.Fin = true;

            return frames;
        }
    }
}
