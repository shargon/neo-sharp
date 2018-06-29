﻿using NeoSharp.Core.Caching;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Models;
using NeoSharp.Core.Persistence;
using NeoSharp.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NeoSharp.Core.Persistence.Contexts;

namespace NeoSharp.Core.Blockchain
{
    public class Blockchain : IDisposable, IBlockchain
    {
        private readonly IBlockHeaderContext _blockHeaderContext;

        //private readonly IRepository _repository;
        public static event EventHandler<Block> PersistCompleted;

        /// <summary>
        /// Memory pool
        /// </summary>
        public StampedPool<UInt256, Transaction> MemoryPool { get; } =
            new StampedPool<UInt256, Transaction>(PoolMaxBehaviour.RemoveFromEnd, 50_000, tx => tx.Value.Hash, TransactionComparer);

        /// <summary>
        /// The interval at which each block is generated, in seconds
        /// </summary>
        public const uint SecondsPerBlock = 15;
        public const uint DecrementInterval = 2000000;
        public const uint MaxValidators = 1024;
        public static readonly uint[] GenerationAmount = { 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        /// <summary>
        /// Generate interval for each block
        /// </summary>
        public static readonly TimeSpan TimePerBlock = TimeSpan.FromSeconds(SecondsPerBlock);

        /// <summary>
        /// TODO: write desc
        /// </summary>
        public static readonly ECPoint[] StandbyValidators = new ECPoint[0]; // read from Config.StandbyValidators.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();

        /// <summary>
        /// GenesisBlock
        /// </summary>
        public static readonly Block GenesisBlock = new Block
        {
            PreviousBlockHash = UInt256.Zero,
            Timestamp = new DateTime(2016, 7, 15, 15, 8, 21, DateTimeKind.Utc).ToTimestamp(),
            Index = 0,
            ConsensusData = 2083236893, //Pay tribute to Bitcoin
            NextConsensus = GetConsensusAddress(StandbyValidators),
            Script = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new byte[0] // new[] { (byte)OpCode.PUSHT }
            },
            Transactions = new Transaction[]
            {
                //new MinerTransaction
                //{
                //    Nonce = 2083236893,
                //    Attributes = new TransactionAttribute[0],
                //    Inputs = new CoinReference[0],
                //    Outputs = new TransactionOutput[0],
                //    Scripts = new Witness[0]
                //},
                //GoverningToken,
                //UtilityToken,
                //new IssueTransaction
                //{
                //    Attributes = new TransactionAttribute[0],
                //    Inputs = new CoinReference[0],
                //    Outputs = new[]
                //    {
                //        new TransactionOutput
                //        {
                //            AssetId = GoverningToken.Hash,
                //            Value = GoverningToken.Amount,
                //            ScriptHash = Contract.CreateMultiSigRedeemScript(StandbyValidators.Length / 2 + 1, StandbyValidators).ToScriptHash()
                //        }
                //    },
                //    Scripts = new[]
                //    {
                //        new Witness
                //        {
                //            InvocationScript = new byte[0],
                //            VerificationScript = new[] { (byte)OpCode.PUSHT }
                //        }
                //    }
                //}
            }
        };

        public Blockchain(IBlockHeaderContext blockHeaderContext)
        {
            _blockHeaderContext = blockHeaderContext;

            // TODO: Uncomment when we figure out transactions in genesis block
            // GenesisBlock.MerkleRoot = MerkleTree.ComputeRoot(GenesisBlock.Transactions.Select(p => p.Hash).ToArray());

            CurrentBlock = GenesisBlock;
            LastBlockHeader = GenesisBlock;
        }

        public Block CurrentBlock { get; private set; }

        public BlockHeader LastBlockHeader { get; private set; }

        static int TransactionComparer(Stamp<Transaction> a, Stamp<Transaction> b)
        {
            int c = 0;// TODO: by fee a.Value.NetworkFee.CompareTo(b.Value.NetworkFee);
            if (c == 0)
            {
                // TODO: Check ASC or DESC

                return a.Date.CompareTo(b.Date);
            }

            return c;
        }

        /// <summary>
        /// Add the specified block to the blockchain
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool AddBlock(Block block)
        {
            // TODO: hook up persistence here
            CurrentBlock = block;
            LastBlockHeader = block;
            return true;
        }

        /// <summary>
        /// Add the specified block headers to the blockchain
        /// </summary>
        /// <param name="blockHeaders"></param>
        public void AddBlockHeaders(IEnumerable<BlockHeader> blockHeaders)
        {
            // TODO: hook up persistence here
            if (blockHeaders.Any())
            {
                LastBlockHeader = blockHeaders.OrderBy(h => h.Index).Last();
            }
        }

        public static Fixed8 CalculateBonus(IEnumerable<CoinReference> inputs, bool ignoreClaimed = true)
        {
            return Fixed8.Zero;
            //List<SpentCoin> unclaimed = new List<SpentCoin>();
            //foreach (var group in inputs.GroupBy(p => p.PrevHash))
            //{
            //    Dictionary<ushort, SpentCoin> claimable = Default.GetUnclaimed(group.Key);
            //    if (claimable == null || claimable.Count == 0)
            //        if (ignoreClaimed)
            //            continue;
            //        else
            //            throw new ArgumentException();
            //    foreach (CoinReference claim in group)
            //    {
            //        if (!claimable.TryGetValue(claim.PrevIndex, out SpentCoin claimed))
            //            if (ignoreClaimed)
            //                continue;
            //            else
            //                throw new ArgumentException();
            //        unclaimed.Add(claimed);
            //    }
            //}
            //return CalculateBonusInternal(unclaimed);
        }

        public static Fixed8 CalculateBonus(IEnumerable<CoinReference> inputs, uint height_end)
        {
            return Fixed8.Zero;
            //List<SpentCoin> unclaimed = new List<SpentCoin>();
            //foreach (var group in inputs.GroupBy(p => p.PrevHash))
            //{
            //    Transaction tx = Default.GetTransactionByHash(group.Key, out int height_start);
            //    if (tx == null) throw new ArgumentException();
            //    if (height_start == height_end) continue;
            //    foreach (CoinReference claim in group)
            //    {
            //        if (claim.PrevIndex >= tx.Outputs.Length || !tx.Outputs[claim.PrevIndex].AssetId.Equals(GoverningToken.Hash))
            //            throw new ArgumentException();
            //        unclaimed.Add(new SpentCoin
            //        {
            //            Output = tx.Outputs[claim.PrevIndex],
            //            StartHeight = (uint)height_start,
            //            EndHeight = height_end
            //        });
            //    }
            //}
            //return CalculateBonusInternal(unclaimed);
        }

        //private static Fixed8 CalculateBonusInternal(IEnumerable<SpentCoin> unclaimed)
        //{
        //    Fixed8 amount_claimed = Fixed8.Zero;
        //    foreach (var group in unclaimed.GroupBy(p => new { p.StartHeight, p.EndHeight }))
        //    {
        //        uint amount = 0;
        //        uint ustart = group.Key.StartHeight / DecrementInterval;
        //        if (ustart < GenerationAmount.Length)
        //        {
        //            uint istart = group.Key.StartHeight % DecrementInterval;
        //            uint uend = group.Key.EndHeight / DecrementInterval;
        //            uint iend = group.Key.EndHeight % DecrementInterval;
        //            if (uend >= GenerationAmount.Length)
        //            {
        //                uend = (uint)GenerationAmount.Length;
        //                iend = 0;
        //            }
        //            if (iend == 0)
        //            {
        //                uend--;
        //                iend = DecrementInterval;
        //            }
        //            while (ustart < uend)
        //            {
        //                amount += (DecrementInterval - istart) * GenerationAmount[ustart];
        //                ustart++;
        //                istart = 0;
        //            }
        //            amount += (iend - istart) * GenerationAmount[ustart];
        //        }
        //        amount += (uint)(Default.GetSysFeeAmount(group.Key.EndHeight - 1) - (group.Key.StartHeight == 0 ? 0 : Default.GetSysFeeAmount(group.Key.StartHeight - 1)));
        //        amount_claimed += group.Sum(p => p.Value) / 100000000 * amount;
        //    }
        //    return amount_claimed;
        //}

        /// <summary>
        /// Determine whether the specified block is contained in the blockchain
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public bool ContainsBlock(UInt256 hash)
        {
            return false;
        }

        /// <summary>
        /// Determine whether the specified transaction is included in the blockchain
        /// </summary>
        /// <param name="hash">Transaction hash</param>
        /// <returns>Return true if the specified transaction is included</returns>
        public bool ContainsTransaction(UInt256 hash)
        {
            return false;
        }


        public bool ContainsUnspent(CoinReference input)
        {
            return ContainsUnspent(input.PrevHash, input.PrevIndex);
        }

        public bool ContainsUnspent(UInt256 hash, ushort index)
        {
            return false;
        }

        public MetaDataCache<T> GetMetaData<T>() where T : class, ISerializable, new()
        {
            return null;
        }

        //public DataCache<TKey, TValue> GetStates<TKey, TValue>()
        //    where TKey : IEquatable<TKey>, ISerializable, new()
        //    where TValue : StateBase, ICloneable<TValue>, new();

        //public void Dispose();

        //public AccountState GetAccountState(UInt160 script_hash);

        //public AssetState GetAssetState(UInt256 asset_id);

        /// <summary>
        /// Return the corresponding block information according to the specified height
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public Block GetBlock(uint height)
        {
            UInt256 hash = GetBlockHash(height);
            if (hash == null) return null;
            return GetBlock(hash);
        }

        /// <summary>
        /// Return the corresponding block information according to the specified height
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Block GetBlock(UInt256 hash)
        {
            return null;
        }

        public Task<IReadOnlyCollection<Block>> GetBlocks(IReadOnlyCollection<UInt256> blockHashes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the hash of the corresponding block based on the specified height
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public UInt256 GetBlockHash(uint height)
        {
            return UInt256.Zero;
        }

        //public ContractState GetContract(UInt160 hash);

        //public IEnumerable<ValidatorState> GetEnrollments();

        /// <summary>
        /// Return the corresponding block header information according to the specified height
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public BlockHeader GetBlockHeader(uint height)
        {
            //TODO: read from repo
            return new BlockHeader();
        }

        /// <summary>
        /// Returns the corresponding block header information according to the specified hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public BlockHeader GetBlockHeader(UInt256 hash)
        {
            //TODO: read from repo
            return new BlockHeader();
        }

        /// <summary>
        /// Get the contractor's contract address
        /// </summary>
        /// <param name="validators"></param>
        /// <returns></returns>
        public static UInt160 GetConsensusAddress(ECPoint[] validators)
        {
            return UInt160.Zero;
            // return Contract.CreateMultiSigRedeemScript(validators.Length - (validators.Length - 1) / 3, validators).ToScriptHash();
        }

        private readonly List<ECPoint> _validators = new List<ECPoint>();

        public ECPoint[] GetValidators()
        {
            lock (_validators)
            {
                if (_validators.Count == 0)
                {
                    _validators.AddRange(GetValidators(Enumerable.Empty<Transaction>()));
                }
                return _validators.ToArray();
            }
        }

        public virtual IEnumerable<ECPoint> GetValidators(IEnumerable<Transaction> others)
        {
            return Enumerable.Empty<ECPoint>();
            //DataCache<UInt160, AccountState> accounts = GetStates<UInt160, AccountState>();
            //DataCache<ECPoint, ValidatorState> validators = GetStates<ECPoint, ValidatorState>();
            //MetaDataCache<ValidatorsCountState> validators_count = GetMetaData<ValidatorsCountState>();
            //foreach (Transaction tx in others)
            //{
            //    foreach (TransactionOutput output in tx.Outputs)
            //    {
            //        AccountState account = accounts.GetAndChange(output.ScriptHash, () => new AccountState(output.ScriptHash));
            //        if (account.Balances.ContainsKey(output.AssetId))
            //            account.Balances[output.AssetId] += output.Value;
            //        else
            //            account.Balances[output.AssetId] = output.Value;
            //        if (output.AssetId.Equals(GoverningToken.Hash) && account.Votes.Length > 0)
            //        {
            //            foreach (ECPoint pubkey in account.Votes)
            //                validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += output.Value;
            //            validators_count.GetAndChange().Votes[account.Votes.Length - 1] += output.Value;
            //        }
            //    }
            //    foreach (var group in tx.Inputs.GroupBy(p => p.PrevHash))
            //    {
            //        Transaction tx_prev = GetTransactionByHash(group.Key, out int height);
            //        foreach (CoinReference input in group)
            //        {
            //            TransactionOutput out_prev = tx_prev.Outputs[input.PrevIndex];
            //            AccountState account = accounts.GetAndChange(out_prev.ScriptHash);
            //            if (out_prev.AssetId.Equals(GoverningToken.Hash))
            //            {
            //                if (account.Votes.Length > 0)
            //                {
            //                    foreach (ECPoint pubkey in account.Votes)
            //                    {
            //                        ValidatorState validator = validators.GetAndChange(pubkey);
            //                        validator.Votes -= out_prev.Value;
            //                        if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
            //                            validators.Delete(pubkey);
            //                    }
            //                    validators_count.GetAndChange().Votes[account.Votes.Length - 1] -= out_prev.Value;
            //                }
            //            }
            //            account.Balances[out_prev.AssetId] -= out_prev.Value;
            //        }
            //    }
            //    switch (tx)
            //    {
            //        case StateTransaction tx_state:
            //            foreach (StateDescriptor descriptor in tx_state.Descriptors)
            //                switch (descriptor.Type)
            //                {
            //                    case StateType.Account:
            //                        ProcessAccountStateDescriptor(descriptor, accounts, validators, validators_count);
            //                        break;
            //                    case StateType.Validator:
            //                        ProcessValidatorStateDescriptor(descriptor, validators);
            //                        break;
            //                }
            //            break;
            //    }
            //}

            //int count = (int)validators_count.Get().Votes.Select((p, i) => new
            //{
            //    Count = i,
            //    Votes = p
            //}).Where(p => p.Votes > Fixed8.Zero).ToArray().WeightedFilter(0.25, 0.75, p => p.Votes.GetData(), (p, w) => new
            //{
            //    p.Count,
            //    Weight = w
            //}).WeightedAverage(p => p.Count, p => p.Weight);
            //count = Math.Max(count, StandbyValidators.Length);
            //HashSet<ECPoint> sv = new HashSet<ECPoint>(StandbyValidators);
            //ECPoint[] pubkeys = validators.Find().Select(p => p.Value).Where(p => (p.Registered && p.Votes > Fixed8.Zero) || sv.Contains(p.PublicKey)).OrderByDescending(p => p.Votes).ThenBy(p => p.PublicKey).Select(p => p.PublicKey).Take(count).ToArray();
            //IEnumerable<ECPoint> result;
            //if (pubkeys.Length == count)
            //{
            //    result = pubkeys;
            //}
            //else
            //{
            //    HashSet<ECPoint> hashSet = new HashSet<ECPoint>(pubkeys);
            //    for (int i = 0; i < StandbyValidators.Length && hashSet.Count < count; i++)
            //        hashSet.Add(StandbyValidators[i]);
            //    result = hashSet;
            //}
            //return result.OrderBy(p => p);
        }

        /// <summary>
        /// Returns the information for the next block based on the specified hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Block GetNextBlock(UInt256 hash)
        {
            return null;
        }

        /// <summary>
        /// Returns the hash value of the next block based on the specified hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public UInt256 GetNextBlockHash(UInt256 hash)
        {
            return UInt256.Zero;
        }

        //byte[] IScriptTable.GetScript(byte[] script_hash)
        //{
        //    return GetContract(new UInt160(script_hash)).Script;
        //}

        //public StorageItem GetStorageItem(StorageKey key);

        /// <summary>
        /// Returns the total amount of system costs contained in the corresponding block and all previous blocks based on the specified block height
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual long GetSysFeeAmount(uint height)
        {
            return GetSysFeeAmount(GetBlockHash(height));
        }

        /// <summary>
        /// Returns the total amount of system charges contained in the corresponding block and all previous blocks based on the specified block hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public long GetSysFeeAmount(UInt256 hash)
        {
            return 0;
        }

        /// <summary>
        /// Returns the corresponding transaction information according to the specified hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Transaction GetTransaction(UInt256 hash)
        {
            return GetTransaction(hash, out _);
        }

        /// <summary>
        /// Return the corresponding transaction information and the height of the block where the transaction is located according to the specified hash value
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Transaction GetTransaction(UInt256 hash, out int height)
        {
            height = 0;
            return null;
        }

        public Task<IReadOnlyCollection<Transaction>> GetTransactions(IReadOnlyCollection<UInt256> transactionHashes)
        {
            throw new NotImplementedException();
        }

        //public Dictionary<ushort, SpentCoin> GetUnclaimed(UInt256 hash);

        /// <summary>
        /// Get the corresponding unspent assets based on the specified hash value and index
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            return null;
        }

        public IEnumerable<TransactionOutput> GetUnspent(UInt256 hash)
        {
            return Enumerable.Empty<TransactionOutput>();
        }

        /// <summary>
        /// Determine if the transaction is double
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public bool IsDoubleSpend(Transaction tx)
        {
            return false;
        }

        /// <summary>
        /// Called after the block was written to the repository
        /// </summary>
        /// <param name="block">区块</param>
        protected void OnPersistCompleted(Block block)
        {
            lock (_validators)
            {
                _validators.Clear();
            }

            PersistCompleted?.Invoke(this, block);
        }

        //protected void ProcessAccountStateDescriptor(StateDescriptor descriptor, DataCache<UInt160, AccountState> accounts, DataCache<ECPoint, ValidatorState> validators, MetaDataCache<ValidatorsCountState> validators_count)
        //{
        //    UInt160 hash = new UInt160(descriptor.Key);
        //    AccountState account = accounts.GetAndChange(hash, () => new AccountState(hash));
        //    switch (descriptor.Field)
        //    {
        //        case "Votes":
        //            Fixed8 balance = account.GetBalance(GoverningToken.Hash);
        //            foreach (ECPoint pubkey in account.Votes)
        //            {
        //                ValidatorState validator = validators.GetAndChange(pubkey);
        //                validator.Votes -= balance;
        //                if (!validator.Registered && validator.Votes.Equals(Fixed8.Zero))
        //                    validators.Delete(pubkey);
        //            }
        //            ECPoint[] votes = descriptor.Value.AsSerializableArray<ECPoint>().Distinct().ToArray();
        //            if (votes.Length != account.Votes.Length)
        //            {
        //                ValidatorsCountState count_state = validators_count.GetAndChange();
        //                if (account.Votes.Length > 0)
        //                    count_state.Votes[account.Votes.Length - 1] -= balance;
        //                if (votes.Length > 0)
        //                    count_state.Votes[votes.Length - 1] += balance;
        //            }
        //            account.Votes = votes;
        //            foreach (ECPoint pubkey in account.Votes)
        //                validators.GetAndChange(pubkey, () => new ValidatorState(pubkey)).Votes += balance;
        //            break;
        //    }
        //}

        //protected void ProcessValidatorStateDescriptor(StateDescriptor descriptor, DataCache<ECPoint, ValidatorState> validators)
        //{
        //    ECPoint pubkey = ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1);
        //    ValidatorState validator = validators.GetAndChange(pubkey, () => new ValidatorState(pubkey));
        //    switch (descriptor.Field)
        //    {
        //        case "Registered":
        //            validator.Registered = BitConverter.ToBoolean(descriptor.Value, 0);
        //            break;
        //    }
        //}
        public void Dispose()
        {
        }
    }
}