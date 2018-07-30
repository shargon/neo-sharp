using NeoSharp.Application.Attributes;
using NeoSharp.Core.Network;

namespace NeoSharp.Application.Client
{
    public partial class Prompt : IPrompt
    {
        /// <summary>
        /// Nodes
        /// </summary>
        [PromptCommand("nodes", Category = "Network", Help = "Get nodes information")]
        private void NodesCommand()
        {
            var peers = _server.ConnectedPeers;

            _consoleWriter.WriteLine("Connected: " + peers.Count);

            foreach (var peer in peers)
            {
                _consoleWriter.WriteLine(peer.ToString());
            }
        }

        /// <summary>
        /// Start network
        /// </summary>
        [PromptCommand("network start", Category = "Network")]
        // ReSharper disable once UnusedMember.Local
        private void NetworkStartCommand([PromptHideHelpCommand]INetworkManager networkManager)
        {
            networkManager?.StartNetwork();
        }

        /// <summary>
        /// Stop network
        /// </summary>
        [PromptCommand("network stop", Category = "Network")]
        private void NetworkStopCommand([PromptHideHelpCommand] INetworkManager networkManager)
        {
            networkManager?.StopNetwork();
        }
    }
}