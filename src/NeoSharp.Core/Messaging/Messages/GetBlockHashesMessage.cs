﻿using NeoSharp.BinarySerialization;
using NeoSharp.Core.Types;

namespace NeoSharp.Core.Messaging.Messages
{
    public class GetBlockHashesMessage : Message<GetBlocksPayload>
    {
        public GetBlockHashesMessage(UInt256 hashStart)
        {
            Command = MessageCommand.getblocks;
            Payload = new GetBlocksPayload
            {
                HashStart = hashStart == null ? new UInt256[] { } : new[] { hashStart }
            };
        }
    }

    public class GetBlocksPayload
    {
        // TODO: Why is it an array if it is always initialized with a single value?

        [BinaryProperty(0)]
        public UInt256[] HashStart;

        [BinaryProperty(1)]
        public UInt256 HashStop = UInt256.Zero;
    }
}