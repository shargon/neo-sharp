﻿using System.Collections.Generic;

namespace NeoSharp.Core.Network
{
    public interface IServer : IBroadcast
    {
        /// <summary>
        /// Connected peers
        /// </summary>
        IReadOnlyCollection<IPeer> ConnectedPeers { get; }

        /// <summary>
        /// Start server
        /// </summary>
        void Start();

        /// <summary>
        /// Stop server
        /// </summary>
        void Stop();

        /// <summary>
        /// Connect to peers
        /// </summary>
        /// <param name="endPoints">The endpoints of peers</param>
        void ConnectToPeers(params EndPoint[] endPoints);
    }
}