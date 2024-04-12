﻿using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace Thirdweb
{
    public class ThirdwebContract
    {
        internal ThirdwebClient Client { get; private set; }
        internal string Address { get; private set; }
        internal BigInteger Chain { get; private set; }
        internal string Abi { get; private set; }

        private ThirdwebContract(ThirdwebClient client, string address, BigInteger chain, string abi)
        {
            Client = client;
            Address = address;
            Chain = chain;
            Abi = abi;
        }

        public static async Task<ThirdwebContract> Create(ThirdwebClient client, string address, BigInteger chain, string abi = null)
        {
            if (client == null)
            {
                throw new ArgumentException("Client must be provided");
            }

            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Address must be provided");
            }

            if (chain == 0)
            {
                throw new ArgumentException("Chain must be provided");
            }

            abi ??= await FetchAbi(address, chain);
            return new ThirdwebContract(client, address, chain, abi);
        }

        public static async Task<string> FetchAbi(string address, BigInteger chainId)
        {
            var url = $"https://contract.thirdweb.com/abi/{chainId}/{address}";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                _ = response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static async Task<T> Read<T>(ThirdwebContract contract, string method, params object[] parameters)
        {
            var rpc = ThirdwebRPC.GetRpcInstance(contract.Client, contract.Chain);

            var service = new Nethereum.Contracts.Contract(null, contract.Abi, contract.Address);
            var function = service.GetFunction(method);
            var data = function.GetData(parameters);

            var resultData = await rpc.SendRequestAsync<string>("eth_call", new { to = contract.Address, data = data, }, "latest");
            return function.DecodeTypeOutput<T>(resultData);
        }

        public static async Task<ThirdwebTransaction> Prepare(IThirdwebWallet wallet, ThirdwebContract contract, string method, BigInteger weiValue, params object[] parameters)
        {
            var service = new Nethereum.Contracts.Contract(null, contract.Abi, contract.Address);
            var function = service.GetFunction(method);
            var data = function.GetData(parameters);
            var transaction = new TransactionInput
            {
                From = await wallet.GetAddress(),
                To = contract.Address,
                Data = data,
                Value = new HexBigInteger(weiValue),
            };

            return await ThirdwebTransaction.Create(contract.Client, wallet, transaction, contract.Chain);
        }

        public static async Task<TransactionReceipt> Write(IThirdwebWallet wallet, ThirdwebContract contract, string method, BigInteger weiValue, params object[] parameters)
        {
            var thirdwebTx = await Prepare(wallet, contract, method, weiValue, parameters);
            return await ThirdwebTransaction.SendAndWaitForTransactionReceipt(thirdwebTx);
        }
    }
}
