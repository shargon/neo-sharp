﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NeoSharp.Application.Attributes;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Models;
using NeoSharp.Core.SmartContract.ContractParameters;
using NeoSharp.Core.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeoSharp.Application.Client
{
    public partial class Prompt : IPrompt
    {
        // TODO: invoke and invokefunction => ContractParameter json serializable/deserializable acording to NEO

        class SendManyParams
        {
            [JsonProperty("asset")]
            public UInt256 Asset { get; set; }

            [JsonProperty("address")]
            public UInt160 Address { get; set; }

            [JsonProperty("value")]
            public BigInteger Value { get; set; }
        }

        #region Base calls

        /// <summary>
        /// Make rpc call
        /// </summary>
        private Task RpcCallCommand(IPEndPoint endPoint, string method, string parameters = null)
        {
            return RpcCallCommand<object>(endPoint, method, parameters, false);
        }

        /// <summary>
        /// Make rpc call
        /// </summary>
        private async Task RpcCallCommand<T>(IPEndPoint endPoint, string method, string parameters = null, bool deserializeResult = false)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                parameters = "[]";
            }

            using (HttpClient wb = new HttpClient())
            {
                var content = new StringContent
                    (
                    "{\"jsonrpc\": \"2.0\", \"method\": \"" + method + "\", \"params\": " + parameters + ", \"id\":1}", Encoding.UTF8,
                    "application/json"
                    );

                var rest = await wb.PostAsync("http://" + endPoint.Address.ToString() + ":" + endPoint.Port.ToString(), content);

                if (!rest.IsSuccessStatusCode)
                {
                    _consoleWriter.WriteLine(rest.StatusCode + " - " + rest.ReasonPhrase, ConsoleOutputStyle.Error);
                    return;
                }

                var json = JObject.Parse(await rest.Content.ReadAsStringAsync());

                if (deserializeResult)
                {
                    var obj = BinaryDeserializer.Default.Deserialize<T>(json["result"].Value<string>().HexToBytes());

                    _consoleWriter.WriteObject(obj, PromptOutputStyle.json);
                }
                else
                {
                    _consoleWriter.WriteObject(json, PromptOutputStyle.json);
                }
            }
        }

        #endregion

        /// <summary>
        /// Start rpc
        /// </summary>
        [PromptCommand("rpc start", Category = "Rpc")]
        private void RpcStartCommand() => _rpc?.Start();

        /// <summary>
        /// Stop rpc
        /// </summary>
        [PromptCommand("rpc stop", Category = "Rpc")]
        private void RpcStopCommand() => _rpc?.Stop();

        #region Commands

        /// <summary> 
        /// Make rpc call for `getapplicationlog` 
        /// </summary> 
        [PromptCommand("rpc getapplicationlog", Category = "Rpc", Help = "Make rpc calls for getapplicationlog")]
        private Task RpcGetapplicationlogCommand(IPEndPoint endPoint, UInt256 hash)
        {
            return RpcCallCommand(endPoint, "getapplicationlog", "[\"" + hash.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getrawmempool` 
        /// </summary> 
        [PromptCommand("rpc getrawmempool", Category = "Rpc", Help = "Make rpc calls for memorypool")]
        private Task RpcGetrawmempoolCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getrawmempool", null);
        }

        /// <summary> 
        /// Make rpc call for `sendtoaddress` 
        /// </summary> 
        [PromptCommand("rpc sendtoaddress", Category = "Rpc", Help = "Make rpc calls for sendtoaddress")]
        private Task RpcSendtoaddressCommand(IPEndPoint endPoint, UInt256 asset, UInt160 to, BigInteger value, ulong fee = 0, UInt160 changeAddress = null)
        {
            // Serialize acording to (https://github.com/neo-project/neo-cli/blob/master/neo-cli/Network/RPC/RpcServerWithWallet.cs#L105)

            var ls = new List<object>
                {
                    asset.ToString(false),
                    to.ToString(false),
                    value,
                    fee
                };

            if (changeAddress != null) ls.Add(changeAddress.ToString(false));

            return RpcCallCommand(endPoint, "sendtoaddress", ls.ToArray().ToJson(false));
        }

        /// <summary> 
        /// Make rpc call for `sendfrom` 
        /// </summary> 
        [PromptCommand("rpc sendfrom", Category = "Rpc", Help = "Make rpc calls for sendfrom")]
        private Task RpcSendfromCommand(IPEndPoint endPoint, UInt256 asset, UInt160 from, UInt160 to, BigInteger value, ulong fee = 0, UInt160 changeAddress = null)
        {
            // Serialize acording to (https://github.com/neo-project/neo-cli/blob/master/neo-cli/Network/RPC/RpcServerWithWallet.cs#L69)

            var ls = new List<object>
                {
                    asset.ToString(false),
                    from.ToString(false),
                    to.ToString(false),
                    value,
                    fee
                };

            if (changeAddress != null) ls.Add(changeAddress.ToString(false));

            return RpcCallCommand(endPoint, "sendfrom", ls.ToArray().ToJson(false));
        }

        /// <summary> 
        /// Make rpc call for `sendmany` 
        /// </summary> 
        [PromptCommand("rpc sendmany", Category = "Rpc", Help = "Make rpc calls for sendmany")]
        private Task RpcSendmanyCommand(IPEndPoint endPoint, [PromptCommandParameterBody(FromJson = true)] SendManyParams[] addresses)
        {
            return RpcCallCommand(endPoint, "sendmany", addresses.ToJson(false));
        }

        /// <summary> 
        /// Make rpc call for `getbalance` 
        /// </summary> 
        [PromptCommand("rpc getbalance", Category = "Rpc", Help = "Make rpc calls for getbalance")]
        private Task RpcGetbalanceCommand(IPEndPoint endPoint, UInt160 hash)
        {
            return RpcCallCommand(endPoint, "getbalance", "[\"" + hash.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getbalance` 
        /// </summary> 
        [PromptCommand("rpc getbalance", Category = "Rpc", Help = "Make rpc calls for getbalance")]
        private Task RpcGetbalanceCommand(IPEndPoint endPoint, UInt256 hash)
        {
            return RpcCallCommand(endPoint, "getbalance", "[\"" + hash.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `dumpprivkey` 
        /// </summary> 
        [PromptCommand("rpc dumpprivkey", Category = "Rpc", Help = "Make rpc calls for dumpprivkey")]
        private Task RpcDumpprivkeyCommand(IPEndPoint endPoint, UInt160 hash)
        {
            return RpcCallCommand(endPoint, "dumpprivkey", "[\"" + hash.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `listaddress` 
        /// </summary> 
        [PromptCommand("rpc listaddress", Category = "Rpc", Help = "Make rpc calls for listaddress")]
        private Task RpcListaddressCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "listaddress", null);
        }

        /// <summary> 
        /// Make rpc call for `getnewaddress` 
        /// </summary> 
        [PromptCommand("rpc getnewaddress", Category = "Rpc", Help = "Make rpc calls for getnewaddress")]
        private Task RpcGetnewaddressCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getnewaddress", null);
        }

        /// <summary> 
        /// Make rpc call for `getvalidators` 
        /// </summary> 
        [PromptCommand("rpc getvalidators", Category = "Rpc", Help = "Make rpc calls for getvalidators")]
        private Task RpcGetvalidatorsCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getvalidators", null);
        }

        /// <summary> 
        /// Make rpc call for `invokescript` 
        /// </summary> 
        [PromptCommand("rpc invokescript", Category = "Rpc", Help = "Make rpc call for invokescript")]
        private Task RpcInvokescriptCommand(IPEndPoint endPoint, byte[] script)
        {
            return RpcCallCommand(endPoint, "invokescript", "[\"" + script.ToHexString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `invoke` 
        /// </summary> 
        [PromptCommand("rpc invoke", Category = "Rpc", Help = "Make rpc call for invoke")]
        private Task RpcInvokeCommand
            (
            IPEndPoint endPoint, UInt160 scriptHash,
            [PromptCommandParameterBody(FromJson = true)] ContractParameter[] args
            )
        {
            return RpcCallCommand(endPoint, "invokefunction", (new object[] { scriptHash.ToString(false), args }.ToJson(false)));
        }

        /// <summary> 
        /// Make rpc call for `invokefunction` 
        /// </summary> 
        [PromptCommand("rpc invokefunction", Category = "Rpc", Help = "Make rpc call for invokefunction")]
        private Task RpcInvokefunctionCommand
            (
            IPEndPoint endPoint, UInt160 scriptHash, string operation,
            [PromptCommandParameterBody(FromJson = true)] ContractParameter[] args
            )
        {
            return RpcCallCommand(endPoint, "invokefunction", (new object[] { scriptHash.ToString(false), operation, args }.ToJson(false)));
        }

        /// <summary> 
        /// Make rpc call for `sendrawtransaction` 
        /// </summary> 
        [PromptCommand("rpc sendrawtransaction", Category = "Rpc", Help = "Make rpc call for sendrawtransaction")]
        private Task RpcSendrawtransactionCommand(IPEndPoint endPoint, byte[] rawTX)
        {
            return RpcCallCommand(endPoint, "sendrawtransaction", "[\"" + rawTX.ToHexString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `submitblock` 
        /// </summary> 
        [PromptCommand("rpc submitblock", Category = "Rpc", Help = "Make rpc call for submitblock")]
        private Task RpcSubmitblockCommand(IPEndPoint endPoint, byte[] rawBlock)
        {
            return RpcCallCommand(endPoint, "submitblock", "[\"" + rawBlock.ToHexString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getpeers` 
        /// </summary> 
        [PromptCommand("rpc getpeers", Category = "Rpc", Help = "Make rpc calls for getpeers")]
        private Task RpcGetpeersCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getpeers", null);
        }

        /// <summary> 
        /// Make rpc call for `getversion` 
        /// </summary> 
        [PromptCommand("rpc getversion", Category = "Rpc", Help = "Make rpc calls for getversion")]
        private Task RpcGetversionCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getversion", null);
        }

        /// <summary> 
        /// Make rpc call for `validateaddress` 
        /// </summary> 
        [PromptCommand("rpc validateaddress", Category = "Rpc", Help = "Make rpc calls for validateaddress")]
        private Task RpcValidateaddressCommand(IPEndPoint endPoint, string address)
        {
            return RpcCallCommand(endPoint, "validateaddress", "[\"" + address + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getconnectioncount` 
        /// </summary> 
        [PromptCommand("rpc getconnectioncount", Category = "Rpc", Help = "Make rpc calls for getconnectioncount")]
        private Task RpcGetconnectioncountCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getconnectioncount", null);
        }

        /// <summary> 
        /// Make rpc call for `getblocksysfee` 
        /// </summary> 
        [PromptCommand("rpc getblocksysfee", Category = "Rpc", Help = "Make rpc calls for getblocksysfee")]
        private Task RpcGetblocksysfeeCommand(IPEndPoint endPoint, uint height)
        {
            return RpcCallCommand(endPoint, "getblocksysfee", "[" + height.ToString() + "]");
        }

        /// <summary> 
        /// Make rpc call for `getbestblockhash` 
        /// </summary> 
        [PromptCommand("rpc getbestblockhash", Category = "Rpc", Help = "Make rpc calls for getbestblockhash")]
        private Task RpcGetassetstateCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getbestblockhash", null);
        }

        /// <summary> 
        /// Make rpc call for `getblockhash` 
        /// </summary> 
        [PromptCommand("rpc getblockhash", Category = "Rpc", Help = "Make rpc calls for getblockhash")]
        private Task RpcGetblockhashCommand(IPEndPoint endPoint, uint height)
        {
            return RpcCallCommand(endPoint, "getblockhash", "[" + height.ToString() + "]");
        }

        /// <summary>
        /// Make rpc call for `getblockcount` 
        /// </summary> 
        [PromptCommand("rpc getblockcount", Category = "Rpc", Help = "Make rpc calls for getblockcount")]
        private Task RpcGetblockcountCommand(IPEndPoint endPoint)
        {
            return RpcCallCommand(endPoint, "getblockcount", null);
        }

        /// <summary>
        /// Make rpc call for `getstorage` 
        /// </summary> 
        [PromptCommand("rpc getstorage", Category = "Rpc", Help = "Make rpc calls for getstorage")]
        private Task RpcGetstorageCommand(IPEndPoint endPoint, UInt160 contract, byte[] key)
        {
            return RpcCallCommand(endPoint, "getstorage", "[\"" + contract.ToString(false) + "\",\"" + key.ToHexString(false) + "\"]");
        }

        /// <summary>
        /// Make rpc call for `gettxout` 
        /// </summary> 
        [PromptCommand("rpc gettxout", Category = "Rpc", Help = "Make rpc calls for gettxout")]
        private Task RpcGettxoutCommand(IPEndPoint endPoint, UInt256 hash, ushort index)
        {
            return RpcCallCommand(endPoint, "gettxout", "[\"" + hash.ToString(false) + "\"," + index.ToString() + "]");
        }

        /// <summary>
        /// Make rpc call for `getrawtransaction` 
        /// </summary> 
        [PromptCommand("rpc getrawtransaction", Category = "Rpc", Help = "Make rpc calls for getrawtransaction")]
        private Task RpcGetrawtransactionCommand(IPEndPoint endPoint, UInt256 hash, bool deserializeResult = false)
        {
            return RpcCallCommand<Transaction>(endPoint, "getrawtransaction", "[\"" + hash.ToString(false) + "\"]", deserializeResult);
        }

        /// <summary>
        /// Make rpc call for `getblock` 
        /// </summary> 
        [PromptCommand("rpc getblock", Category = "Rpc", Help = "Make rpc calls for getblock")]
        private Task RpcGetblockCommand(IPEndPoint endPoint, uint height, bool deserializeResult = false)
        {
            return RpcCallCommand<Block>(endPoint, "getblock", "[" + height.ToString() + "]", deserializeResult);
        }

        /// <summary> 
        /// Make rpc call for `getcontractstate` 
        /// </summary> 
        [PromptCommand("rpc getcontractstate", Category = "Rpc", Help = "Make rpc calls for getcontractstate")]
        private Task RpcGetcontractstateCommand(IPEndPoint endPoint, UInt160 address)
        {
            return RpcCallCommand(endPoint, "getcontractstate", "[\"" + address.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getaccountstate` 
        /// </summary> 
        [PromptCommand("rpc getaccountstate", Category = "Rpc", Help = "Make rpc calls for getaccountstate")]
        private Task RpcGetaccountstateCommand(IPEndPoint endPoint, UInt160 address)
        {
            return RpcCallCommand(endPoint, "getaccountstate", "[\"" + address.ToString(false) + "\"]");
        }

        /// <summary> 
        /// Make rpc call for `getassetstate` 
        /// </summary> 
        [PromptCommand("rpc getassetstate", Category = "Rpc", Help = "Make rpc calls for getassetstate")]
        private Task RpcGetassetstateCommand(IPEndPoint endPoint, UInt256 address)
        {
            return RpcCallCommand(endPoint, "getassetstate", "[\"" + address.ToString(false) + "\"]");
        }

        #endregion
    }
}