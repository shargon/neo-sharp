﻿using System;
using System.Threading.Tasks;
using RocksDbSharp;

namespace NeoSharp.Persistence.RocksDB
{
    public class RocksDbContext : IRocksDbContext
    {
        #region Private Fields 
        private readonly RocksDb _rocksDb;
        #endregion

        #region Constructor 
        public RocksDbContext(RocksDbConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // Initialize RocksDB (Connection String is the path to use)
            var options = new DbOptions().SetCreateIfMissing();
            // TODO: please avoid sync IO in constructor -> Open connection with the first operation for now
            this._rocksDb = RocksDb.Open(options, config.FilePath);
        }
        #endregion

        #region IRocksDbContext implementation 
        public Task Save(byte[] key, byte[] content)
        {
            return Task.Run(() => this._rocksDb.Put(key, content));
        }

        public Task<byte[]> Get(byte[] key)
        {
            return Task.Run(() => this._rocksDb.Get(key));
        }

        public void Dispose()
        {
            this._rocksDb?.Dispose();
        }
        #endregion
    }
}