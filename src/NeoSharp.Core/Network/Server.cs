using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Messaging.Messages;

namespace NeoSharp.Core.Network
{
    public class Server : IServer, IDisposable
    {
        #region Variables
        private readonly ILogger<Server> _logger;
        private readonly IServerContext _serverContext;
        private readonly IPeerMessageListener _peerMessageListener;
        private readonly IPeerFactory _peerFactory;
        private readonly IPeerListener _peerListener;

        // if we successfully connect with a peer it is inserted into this list
        private readonly ConcurrentBag<IPeer> _connectedPeers;

        // if we can't connect to a peer it is inserted into this list
        // ReSharper disable once NotAccessedField.Local
        private readonly IList<IPEndPoint> _failedPeers;
        private readonly EndPoint[] _peerEndPoints;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">Network config</param>
        /// <param name="peerFactory">PeerFactory</param>
        /// <param name="peerListener">PeerListener</param>
        /// <param name="logger">Logger</param>
        /// <param name="serverContext">Server context</param>
        /// <param name="peerMessageListener"></param>
        public Server(
            NetworkConfig config,
            IPeerFactory peerFactory,
            IPeerListener peerListener,
            ILogger<Server> logger,
            IServerContext serverContext,
            IPeerMessageListener peerMessageListener)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _peerFactory = peerFactory ?? throw new ArgumentNullException(nameof(peerFactory));
            _peerListener = peerListener ?? throw new ArgumentNullException(nameof(peerListener));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverContext = serverContext ?? throw new ArgumentNullException(nameof(serverContext));
            _peerMessageListener = peerMessageListener ?? throw new ArgumentNullException(nameof(peerMessageListener));

            _peerListener.OnPeerConnected += PeerConnected;

            _connectedPeers = new ConcurrentBag<IPeer>();
            _failedPeers = new List<IPEndPoint>();

            // TODO: Change after port forwarding implementation
            _peerEndPoints = config.PeerEndPoints;
        }
        #endregion

        #region IServer implementation

        /// <inheritdoc />
        public IReadOnlyCollection<IPeer> ConnectedPeers => _connectedPeers.ToArray();

        /// <inheritdoc />
        public void Start()
        {
            Stop();

            // connect to peers
            ConnectToPeers(_peerEndPoints);

            // listen for peers
            _peerListener.Start();
        }

        /// <inheritdoc />
        public void Stop()
        {
            _peerListener.Stop();

            DisconnectPeers();

            _peerMessageListener.StopListenAllPeers();
        }

        /// <inheritdoc />
        public void ConnectToPeers(params EndPoint[] endPoints)
        {
            Parallel.ForEach(endPoints, async ep =>
            {
                try
                {
                    var peer = await _peerFactory.ConnectTo(ep);
                    PeerConnected(this, peer);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Something went wrong with {ep}. Exception: {ex}");
                }
            });
        }

        /// <inheritdoc />
        public async Task SendBroadcast(Message message, Func<IPeer, bool> filter = null)
        {
            Parallel.ForEach(_connectedPeers, async (peer) =>
            {
                // Check filter

                if (filter != null && !filter(peer)) return;

                // Send

                await peer.Send(message);
            });

            await Task.CompletedTask;
        }
        #endregion

        #region IDisposable Implementation 
        /// <inheritdoc />
        public void Dispose()
        {
            Stop();
            _peerListener.OnPeerConnected -= PeerConnected;
        }
        #endregion

        #region Private Methods 
        /// <summary>
        /// Peer connected Event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="peer">Peer</param>
        private void PeerConnected(object sender, IPeer peer)
        {
            try
            {
                _connectedPeers.Add(peer);

                _peerMessageListener.StartListen(peer);

                // Initiate handshake
                peer.Send(new VersionMessage(_serverContext.Version));
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Something went wrong with {peer}. Exception: {e}");
                peer.Disconnect();
            }
        }

        /// <summary>
        /// Send disconnect to all current Peers
        /// </summary>
        private void DisconnectPeers()
        {
            foreach (var peer in _connectedPeers)
            {
                peer.Disconnect();
            }

            _connectedPeers.Clear();
        }

        #endregion
    }
}