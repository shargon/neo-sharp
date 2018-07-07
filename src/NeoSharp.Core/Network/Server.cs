using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Messaging;
using NeoSharp.Core.Network.Security;

namespace NeoSharp.Core.Network
{
    public class Server : IServer, IDisposable
    {
        #region Variables

        private bool _isRunning;
        private readonly ILogger<Server> _logger;
        private readonly IPeerMessageListener _peerMessageListener;
        private readonly IPeerFactory _peerFactory;
        private readonly IPeerListener _peerListener;
        private readonly NetworkAcl _acl;

        // if we successfully connect with a peer it is inserted into this list
        private readonly ConcurrentBag<IPeer> _connectedPeers;

        // if we can't connect to a peer it is inserted into this list
        // ReSharper disable once NotAccessedField.Local
        private readonly IList<IPEndPoint> _failedPeers;
        private readonly EndPoint[] _peerEndPoints;
        private CancellationTokenSource _messageListenerTokenSource;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">Network configuration</param>
        /// <param name="aclLoader">ACL loader to define access</param>
        /// <param name="peerFactory">Factory to create peers from endpoints</param>
        /// <param name="peerListener">Listener to accept peer connections</param>
        /// <param name="peerMessageListener">PeerMessageListener</param>
        /// <param name="logger">Logger</param>
        public Server(
            NetworkConfig config,
            INetworkAclLoader aclLoader,
            IPeerFactory peerFactory,
            IPeerListener peerListener,
            IPeerMessageListener peerMessageListener,
            ILogger<Server> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _peerFactory = peerFactory ?? throw new ArgumentNullException(nameof(peerFactory));
            _peerListener = peerListener ?? throw new ArgumentNullException(nameof(peerListener));

            if (aclLoader == null) throw new ArgumentNullException(nameof(aclLoader));
            _acl = aclLoader.Load(config.AclConfig) ?? NetworkAcl.Default;

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (_isRunning)
            {
                throw new InvalidOperationException("The server is running. To start it again please stop it before.");
            }

            _messageListenerTokenSource = new CancellationTokenSource();

            // connect to peers
            ConnectToPeers(_peerEndPoints);

            // listen for peers
            _peerListener.Start();

            _isRunning = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _peerListener.Stop();

            _messageListenerTokenSource?.Cancel();

            DisconnectPeers();

            _isRunning = false;
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
                if (_acl.IsAllowed(peer.EndPoint) == false)
                {
                    throw new UnauthorizedAccessException($"The endpoint \"{peer.EndPoint}\" is prohibited by ACL.");
                }

                _connectedPeers.Add(peer);
                _peerMessageListener.StartFor(peer, _messageListenerTokenSource.Token);
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