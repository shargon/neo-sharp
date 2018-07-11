﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoSharp.Core.Blockchain;
using NeoSharp.Core.Caching;
using NeoSharp.Core.Cryptography;
using NeoSharp.Core.Models;
using NeoSharp.Core.Types;

namespace NeoSharp.Core.Test.Messaging.Handlers
{
    class NullBlockchain : IBlockchain
    {
        public Block CurrentBlock { get; } = new Block();

        public BlockHeaderBase LastBlockHeader => CurrentBlock.GetBlockHeaderBase();

        public StampedPool<UInt256, Transaction> MemoryPool => new StampedPool<UInt256, Transaction>(PoolMaxBehaviour.DontAllowMore, 0, x => x.Value.Hash, null);

        public Task InitializeBlockchain()
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddBlock(Block block)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public bool ContainsTransaction(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public bool ContainsUnspent(CoinReference input)
        {
            throw new NotImplementedException();
        }

        public bool ContainsUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public MetaDataCache<T> GetMetaData<T>() where T : class, ISerializable, new()
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetBlock(uint height)
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public Contract GetContract(UInt160 hash)
        {
            throw new NotImplementedException();
        }

        public Asset GetAsset(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Asset> GetAssets()
        {
            yield break;
        }

        public IEnumerable<Contract> GetContracts()
        {
            yield break;
        }

        public Task<IEnumerable<Block>> GetBlocks(IReadOnlyCollection<UInt256> blockHashes)
        {
            throw new NotImplementedException();
        }

        public Task<UInt256> GetBlockHash(uint height)
        {
            throw new NotImplementedException();
        }

        public Task<BlockHeaderBase> GetBlockHeader(uint height)
        {
            throw new NotImplementedException();
        }

        public Task<BlockHeaderBase> GetBlockHeader(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public ECPoint[] GetValidators()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ECPoint> GetValidators(IEnumerable<Transaction> others)
        {
            throw new NotImplementedException();
        }

        public Task<Block> GetNextBlock(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public Task<UInt256> GetNextBlockHash(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetSysFeeAmount(uint height)
        {
            throw new NotImplementedException();
        }

        public long GetSysFeeAmount(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public Task<Transaction> GetTransaction(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public Transaction GetTransaction(UInt256 hash, out int height)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Transaction>> GetTransactions(IReadOnlyCollection<UInt256> transactionHashes)
        {
            throw new NotImplementedException();
        }

        public TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransactionOutput> GetUnspent(UInt256 hash)
        {
            throw new NotImplementedException();
        }

        public bool IsDoubleSpend(Transaction tx)
        {
            throw new NotImplementedException();
        }

        public Task AddBlockHeaders(IEnumerable<BlockHeaderBase> blockHeaders)
        {
            throw new NotImplementedException();
        }
    }
}