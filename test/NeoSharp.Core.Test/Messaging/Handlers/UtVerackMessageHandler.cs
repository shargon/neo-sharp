﻿using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NeoSharp.Core.Blockchain;
using NeoSharp.Core.Messaging.Handlers;
using NeoSharp.Core.Messaging.Messages;
using NeoSharp.Core.Network;
using NeoSharp.Core.Types;
using NeoSharp.TestHelpers;

namespace NeoSharp.Core.Test.Messaging.Handlers
{
    [TestClass]
    public class UtVerAckMessageHandler : TestBase
    {
        [TestMethod]
        public async Task Can_activate_peer_on_verack_receiving()
        {
            // Arrange
            var blockchain = new NullBlockchain();

            AutoMockContainer.Register<IBlockchain>(blockchain);

            var verAckMessage = new VerAckMessage();
            var version = new VersionPayload();
            var peerMock = AutoMockContainer.GetMock<IPeer>();

            peerMock.SetupProperty(x => x.IsReady, false);
            peerMock.SetupProperty(x => x.Version, version);

            blockchain.LastBlockHeader.Index = 1;
            version.CurrentBlockIndex = 1;

            var messageHandler = AutoMockContainer.Get<VerAckMessageHandler>();

            // Act
            await messageHandler.Handle(verAckMessage, peerMock.Object);

            // Assert
            peerMock.Object.IsReady.Should().BeTrue();
        }

        [TestMethod]
        public async Task Can_send_get_block_headers_message_if_peer_block_header_height_is_different()
        {
            // Arrange
            var blockchain = new NullBlockchain();

            AutoMockContainer.Register<IBlockchain>(blockchain);

            var verAckMessage = new VerAckMessage();
            var version = new VersionPayload();
            var peerMock = AutoMockContainer.GetMock<IPeer>();

            peerMock.SetupProperty(x => x.Version, version);

            blockchain.LastBlockHeader.Index = 1;
            blockchain.LastBlockHeader.Hash = UInt256.Zero;
            version.CurrentBlockIndex = 2;

            var messageHandler = AutoMockContainer.Get<VerAckMessageHandler>();

            // Act
            await messageHandler.Handle(verAckMessage, peerMock.Object);

            // Assert
            peerMock.Verify(x => x.Send(It.IsAny<GetBlockHeadersMessage>()), Times.Once);
        }
    }
}