using Nethereum.JsonRpc.Client.RpcMessages;
using Newtonsoft.Json;

namespace Thirdweb.AccountAbstraction
{
    public static class BundlerClient
    {
        // Bundler requests

        public static async Task<EthGetUserOperationReceiptResponse> EthGetUserOperationReceipt(ThirdwebClient client, string bundlerUrl, object requestId, string userOpHash)
        {
            var response = await BundlerRequest(client, bundlerUrl, requestId, "eth_getUserOperationReceipt", userOpHash);
            return JsonConvert.DeserializeObject<EthGetUserOperationReceiptResponse>(response.Result.ToString());
        }

        public static async Task<string> EthSendUserOperation(ThirdwebClient client, string bundlerUrl, object requestId, UserOperationHexified userOp, string entryPoint)
        {
            var response = await BundlerRequest(client, bundlerUrl, requestId, "eth_sendUserOperation", userOp, entryPoint);
            return response.Result.ToString();
        }

        public static async Task<EthEstimateUserOperationGasResponse> EthEstimateUserOperationGas(
            ThirdwebClient client,
            string bundlerUrl,
            object requestId,
            UserOperationHexified userOp,
            string entryPoint
        )
        {
            var response = await BundlerRequest(client, bundlerUrl, requestId, "eth_estimateUserOperationGas", userOp, entryPoint);
            return JsonConvert.DeserializeObject<EthEstimateUserOperationGasResponse>(response.Result.ToString());
        }

        public static async Task<ThirdwebGetUserOperationGasPriceResponse> ThirdwebGetUserOperationGasPrice(ThirdwebClient client, string bundlerUrl, object requestId)
        {
            var response = await BundlerRequest(client, bundlerUrl, requestId, "thirdweb_getUserOperationGasPrice");
            return JsonConvert.DeserializeObject<ThirdwebGetUserOperationGasPriceResponse>(response.Result.ToString());
        }

        // Paymaster requests

        public static async Task<PMSponsorOperationResponse> PMSponsorUserOperation(ThirdwebClient client, string paymasterUrl, object requestId, UserOperationHexified userOp, string entryPoint)
        {
            var response = await BundlerRequest(client, paymasterUrl, requestId, "pm_sponsorUserOperation", userOp, new EntryPointWrapper() { entryPoint = entryPoint });
            try
            {
                return JsonConvert.DeserializeObject<PMSponsorOperationResponse>(response.Result.ToString());
            }
            catch
            {
                return new PMSponsorOperationResponse() { paymasterAndData = response.Result.ToString() };
            }
        }

        public static async Task<ZkPaymasterDataResponse> ZkPaymasterData(ThirdwebClient client, string paymasterUrl, object requestId, ThirdwebTransactionInput txInput)
        {
            var response = await BundlerRequest(client, paymasterUrl, requestId, "zk_paymasterData", txInput);
            try
            {
                return JsonConvert.DeserializeObject<ZkPaymasterDataResponse>(response.Result.ToString());
            }
            catch
            {
                return new ZkPaymasterDataResponse() { paymaster = null, paymasterInput = null };
            }
        }

        public static async Task<ZkBroadcastTransactionResponse> ZkBroadcastTransaction(ThirdwebClient client, string paymasterUrl, object requestId, object txInput)
        {
            var response = await BundlerRequest(client, paymasterUrl, requestId, "zk_broadcastTransaction", txInput);
            return JsonConvert.DeserializeObject<ZkBroadcastTransactionResponse>(response.Result.ToString());
        }

        // Request

        private static async Task<RpcResponseMessage> BundlerRequest(ThirdwebClient client, string url, object requestId, string method, params object[] args)
        {
            var cts = new CancellationTokenSource(client.FetchTimeoutOptions.GetTimeout(TimeoutType.Other));

            var httpClient = client.HttpClient;
            var requestMessage = new RpcRequestMessage(requestId, method, args);
            var requestMessageJson = JsonConvert.SerializeObject(requestMessage);

            var httpContent = new StringContent(requestMessageJson, System.Text.Encoding.UTF8, "application/json");

            ThirdwebHttpResponseMessage httpResponse;
            try
            {
                httpResponse = await httpClient.PostAsync(url, httpContent, cts.Token);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("The request timed out.");
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Bundler Request Failed. Error: {httpResponse.StatusCode} - {await httpResponse.Content.ReadAsStringAsync()}");
            }

            var httpResponseJson = await httpResponse.Content.ReadAsStringAsync();

            var response = JsonConvert.DeserializeObject<RpcResponseMessage>(httpResponseJson);
            return response.Error != null ? throw new Exception($"Bundler Request Failed. Error: {response.Error.Code} - {response.Error.Message} - {response.Error.Data}") : response;
        }
    }
}
