﻿using System;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Models;
using NeoSharp.Core.Persistence;
using RocksDbSharp;

namespace NeoSharp.Persistence.RocksDB
{
    public class RocksDbRepository : IRocksDbRepository, IDisposable
    {
        #region Private Fields

        private readonly RocksDb _rocksDb;
        private readonly IBinarySerializer _serializer;
        private readonly IBinaryDeserializer _deserializer;

        private readonly byte[] _sysCurrentBlockKey = new byte[] { (byte)DataEntryPrefix.SysCurrentBlock };

        #endregion

        #region Constructor

        public RocksDbRepository(RocksDbConfig config, IBinarySerializer serializer, IBinaryDeserializer deserializer)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

            // Initialize RocksDB (Connection String is the path to use)
            var options = new DbOptions().SetCreateIfMissing(true);
            // TODO: please avoid sync IO in constructor -> Open connection with the first operation for now
            _rocksDb = RocksDb.Open(options, config.FilePath);
        }

        #endregion

        #region IRepository Members

        public byte[] GetBlockHashFromHeight(uint height)
        {
            return _rocksDb.Get(BuildKey(DataEntryPrefix.IxHeightToHash, BitConverter.GetBytes(height)));
        }

        public void AddBlockHeader(BlockHeader blockHeader)
        {
            var hash = blockHeader.Hash.ToArray();
            var ix = BitConverter.GetBytes(blockHeader.Index);

            _rocksDb.Put(BuildKey(DataEntryPrefix.DataBlock, hash), _serializer.Serialize(blockHeader));
            _rocksDb.Put(BuildKey(DataEntryPrefix.IxHeightToHash, ix), hash);
        }

        public void AddTransaction(Transaction transaction)
        {
            var hash = transaction.Hash.ToArray();
            _rocksDb.Put(BuildKey(DataEntryPrefix.DataTransaction, hash), _serializer.Serialize(transaction));
        }

        public BlockHeader GetBlockHeader(byte[] hash)
        {
            var rawHeader = _rocksDb.Get(BuildKey(DataEntryPrefix.DataBlock, hash));

            if (rawHeader != null)
            {
                return _deserializer.Deserialize<BlockHeader>(rawHeader);
            }

            return null;
        }

        public uint GetTotalBlockHeight()
        {
            var raw = _rocksDb.Get(_sysCurrentBlockKey);

            if (raw != null) return BitConverter.ToUInt32(raw, 0);

            return uint.MinValue;
        }

        public void SetTotalBlockHeight(uint height)
        {
            _rocksDb.Put(_sysCurrentBlockKey, BitConverter.GetBytes(height));
        }

        public Transaction GetTransaction(byte[] hash)
        {
            var bytes = _rocksDb.Get(BuildKey(DataEntryPrefix.DataTransaction, hash));

            if (bytes == null) return null;

            return _deserializer.Deserialize<Transaction>(bytes);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_rocksDb != null)
            {
                _rocksDb.Dispose();
            }
        }

        #endregion

        #region Private Methods 

        /// <summary>
        /// Builds the concatenated key based on data type and desired key
        /// </summary>
        /// <param name="type">Data type</param>
        /// <param name="key">Desired key</param>
        /// <returns>Resulting key</returns>
        private static byte[] BuildKey(DataEntryPrefix type, byte[] key)
        {
            var len = key.Length;
            var bytes = new byte[len + 1];

            bytes[0] = (byte)type;
            Array.Copy(key, 0, bytes, 1, len);

            return bytes;
        }

        #endregion
    }
}
