using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Newtonsoft.Json;

namespace Thirdweb.AccountAbstraction
{
    public class UserOperationV6
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }

        [Parameter("uint256", "nonce", 2)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("bytes", "initCode", 3)]
        public virtual byte[] InitCode { get; set; }

        [Parameter("bytes", "callData", 4)]
        public virtual byte[] CallData { get; set; }

        [Parameter("uint256", "callGasLimit", 5)]
        public virtual BigInteger CallGasLimit { get; set; }

        [Parameter("uint256", "verificationGasLimit", 6)]
        public virtual BigInteger VerificationGasLimit { get; set; }

        [Parameter("uint256", "preVerificationGas", 7)]
        public virtual BigInteger PreVerificationGas { get; set; }

        [Parameter("uint256", "maxFeePerGas", 8)]
        public virtual BigInteger MaxFeePerGas { get; set; }

        [Parameter("uint256", "maxPriorityFeePerGas", 9)]
        public virtual BigInteger MaxPriorityFeePerGas { get; set; }

        [Parameter("bytes", "paymasterAndData", 10)]
        public virtual byte[] PaymasterAndData { get; set; }

        [Parameter("bytes", "signature", 11)]
        public virtual byte[] Signature { get; set; }
    }

    public class UserOperationV7
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }

        [Parameter("uint256", "nonce", 2)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("address", "factory", 3)]
        public virtual string Factory { get; set; }

        [Parameter("bytes", "factoryData", 4)]
        public virtual byte[] FactoryData { get; set; }

        [Parameter("bytes", "callData", 5)]
        public virtual byte[] CallData { get; set; }

        [Parameter("uint256", "callGasLimit", 6)]
        public virtual BigInteger CallGasLimit { get; set; }

        [Parameter("uint256", "verificationGasLimit", 7)]
        public virtual BigInteger VerificationGasLimit { get; set; }

        [Parameter("uint256", "preVerificationGas", 8)]
        public virtual BigInteger PreVerificationGas { get; set; }

        [Parameter("uint256", "maxFeePerGas", 9)]
        public virtual BigInteger MaxFeePerGas { get; set; }

        [Parameter("uint256", "maxPriorityFeePerGas", 10)]
        public virtual BigInteger MaxPriorityFeePerGas { get; set; }

        [Parameter("address", "paymaster", 11)]
        public virtual string Paymaster { get; set; }

        [Parameter("uint256", "paymasterVerificationGasLimit", 12)]
        public virtual BigInteger PaymasterVerificationGasLimit { get; set; }

        [Parameter("uint256", "paymasterPostOpGasLimit", 13)]
        public virtual BigInteger PaymasterPostOpGasLimit { get; set; }

        [Parameter("bytes", "paymasterData", 14)]
        public virtual byte[] PaymasterData { get; set; }

        [Parameter("bytes", "signature", 15)]
        public virtual byte[] Signature { get; set; }
    }

    public class PackedUserOperation
    {
        [Parameter("address", "sender", 1)]
        public virtual string Sender { get; set; }

        [Parameter("uint256", "nonce", 2)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("bytes", "initCode", 3)]
        public virtual byte[] InitCode { get; set; }

        [Parameter("bytes", "callData", 4)]
        public virtual byte[] CallData { get; set; }

        [Parameter("bytes32", "accountGasLimits", 5)]
        public virtual byte[] AccountGasLimits { get; set; }

        [Parameter("uint256", "preVerificationGas", 6)]
        public virtual BigInteger PreVerificationGas { get; set; }

        [Parameter("bytes32", "gasFees", 7)]
        public virtual byte[] GasFees { get; set; }

        [Parameter("bytes", "paymasterAndData", 8)]
        public virtual byte[] PaymasterAndData { get; set; }

        [Parameter("bytes", "signature", 9)]
        public virtual byte[] Signature { get; set; }
    }

    public class UserOperationHexifiedV6
    {
        public string sender { get; set; }
        public string nonce { get; set; }
        public string initCode { get; set; }
        public string callData { get; set; }
        public string callGasLimit { get; set; }
        public string verificationGasLimit { get; set; }
        public string preVerificationGas { get; set; }
        public string maxFeePerGas { get; set; }
        public string maxPriorityFeePerGas { get; set; }
        public string paymasterAndData { get; set; }
        public string signature { get; set; }
    }

    public class UserOperationHexifiedV7
    {
        public string sender { get; set; }
        public string nonce { get; set; }
        public string factory { get; set; }
        public string factoryData { get; set; }
        public string callData { get; set; }
        public string callGasLimit { get; set; }
        public string verificationGasLimit { get; set; }
        public string preVerificationGas { get; set; }
        public string maxFeePerGas { get; set; }
        public string maxPriorityFeePerGas { get; set; }
        public string paymaster { get; set; }
        public string paymasterVerificationGasLimit { get; set; }
        public string paymasterPostOpGasLimit { get; set; }
        public string paymasterData { get; set; }
        public string signature { get; set; }
    }

    [Function("execute")]
    public class ExecuteFunctionV6 : FunctionMessage
    {
        [Parameter("address", "_target", 1)]
        public virtual string Target { get; set; }

        [Parameter("uint256", "_value", 2)]
        public virtual BigInteger Value { get; set; }

        [Parameter("bytes", "_calldata", 3)]
        public virtual byte[] Calldata { get; set; }
    }

    [Function("execute")]
    public class ExecuteFunctionV7 : FunctionMessage
    {
        [Parameter("bytes32", "mode", 1)]
        public virtual byte[] Mode { get; set; }

        [Parameter("bytes", "executionCalldata", 2)]
        public virtual byte[] ExecutionCalldata { get; set; }
    }

    public class EthEstimateUserOperationGasResponse
    {
        [JsonProperty("preVerificationGas")]
        public string PreVerificationGas { get; set; }

        [JsonProperty("verificationGasLimit")]
        public string VerificationGasLimit { get; set; }

        [JsonProperty("callGasLimit")]
        public string CallGasLimit { get; set; }

        [JsonProperty("paymasterVerificationGasLimit")]
        public string PaymasterVerificationGasLimit { get; set; }

        [JsonProperty("paymasterPostOpGasLimit")]
        public string PaymasterPostOpGasLimit { get; set; }
    }

    public class EthGetUserOperationReceiptResponse
    {
        [JsonProperty("receipt")]
        public ThirdwebTransactionReceipt Receipt { get; set; }
    }

    public class EntryPointWrapper
    {
        [JsonProperty("entryPoint")]
        public string entryPoint { get; set; }
    }

    public class PMSponsorOperationResponse
    {
        [JsonProperty("paymasterAndData")]
        public string PaymasterAndData { get; set; }

        [JsonProperty("paymaster")]
        public string Paymaster { get; set; }

        [JsonProperty("paymasterData")]
        public string PaymasterData { get; set; }

        [JsonProperty("preVerificationGas")]
        public string PreVerificationGas { get; set; }

        [JsonProperty("verificationGasLimit")]
        public string VerificationGasLimit { get; set; }

        [JsonProperty("callGasLimit")]
        public string CallGasLimit { get; set; }

        [JsonProperty("paymasterVerificationGasLimit")]
        public string PaymasterVerificationGasLimit { get; set; }

        [JsonProperty("paymasterPostOpGasLimit")]
        public string PaymasterPostOpGasLimit { get; set; }
    }

    public class ThirdwebGetUserOperationGasPriceResponse
    {
        [JsonProperty("maxFeePerGas")]
        public string MaxFeePerGas { get; set; }

        [JsonProperty("maxPriorityFeePerGas")]
        public string MaxPriorityFeePerGas { get; set; }
    }

    [Event("UserOperationEvent")]
    public class UserOperationEventEventDTO : IEventDTO
    {
        [Parameter("bytes32", "userOpHash", 1, true)]
        public virtual byte[] UserOpHash { get; set; }

        [Parameter("address", "sender", 2, true)]
        public virtual string Sender { get; set; }

        [Parameter("address", "paymaster", 3, true)]
        public virtual string Paymaster { get; set; }

        [Parameter("uint256", "nonce", 4, false)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("bool", "success", 5, false)]
        public virtual bool Success { get; set; }

        [Parameter("uint256", "actualGasCost", 6, false)]
        public virtual BigInteger ActualGasCost { get; set; }

        [Parameter("uint256", "actualGasUsed", 7, false)]
        public virtual BigInteger ActualGasUsed { get; set; }
    }

    [Event("UserOperationRevertReason")]
    public class UserOperationRevertReasonEventDTO : IEventDTO
    {
        [Parameter("bytes32", "userOpHash", 1, true)]
        public virtual byte[] UserOpHash { get; set; }

        [Parameter("address", "sender", 2, true)]
        public virtual string Sender { get; set; }

        [Parameter("uint256", "nonce", 3, false)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("bytes", "revertReason", 4, false)]
        public virtual byte[] RevertReason { get; set; }
    }

    [Struct("SignerPermissionRequest")]
    public class SignerPermissionRequest
    {
        [Parameter("address", "signer", 1)]
        public virtual string Signer { get; set; }

        [Parameter("uint8", "isAdmin", 2)]
        public virtual byte IsAdmin { get; set; }

        [Parameter("address[]", "approvedTargets", 3)]
        public virtual List<string> ApprovedTargets { get; set; }

        [Parameter("uint256", "nativeTokenLimitPerTransaction", 4)]
        public virtual BigInteger NativeTokenLimitPerTransaction { get; set; }

        [Parameter("uint128", "permissionStartTimestamp", 5)]
        public virtual BigInteger PermissionStartTimestamp { get; set; }

        [Parameter("uint128", "permissionEndTimestamp", 6)]
        public virtual BigInteger PermissionEndTimestamp { get; set; }

        [Parameter("uint128", "reqValidityStartTimestamp", 7)]
        public virtual BigInteger ReqValidityStartTimestamp { get; set; }

        [Parameter("uint128", "reqValidityEndTimestamp", 8)]
        public virtual BigInteger ReqValidityEndTimestamp { get; set; }

        [Parameter("bytes32", "uid", 9)]
        public virtual byte[] Uid { get; set; }
    }

    [Struct("AccountMessage")]
    public class AccountMessage
    {
        [Parameter("bytes", "message", 1)]
        public virtual byte[] Message { get; set; }
    }

    [Struct("Transaction")]
    public class ZkSyncAATransaction
    {
        [Parameter("uint256", "txType", 1)]
        public virtual BigInteger TxType { get; set; }

        [Parameter("uint256", "from", 2)]
        public virtual BigInteger From { get; set; }

        [Parameter("uint256", "to", 3)]
        public virtual BigInteger To { get; set; }

        [Parameter("uint256", "gasLimit", 4)]
        public virtual BigInteger GasLimit { get; set; }

        [Parameter("uint256", "gasPerPubdataByteLimit", 5)]
        public virtual BigInteger GasPerPubdataByteLimit { get; set; }

        [Parameter("uint256", "maxFeePerGas", 6)]
        public virtual BigInteger MaxFeePerGas { get; set; }

        [Parameter("uint256", "maxPriorityFeePerGas", 7)]
        public virtual BigInteger MaxPriorityFeePerGas { get; set; }

        [Parameter("uint256", "paymaster", 8)]
        public virtual BigInteger Paymaster { get; set; }

        [Parameter("uint256", "nonce", 9)]
        public virtual BigInteger Nonce { get; set; }

        [Parameter("uint256", "value", 10)]
        public virtual BigInteger Value { get; set; }

        [Parameter("bytes", "data", 11)]
        public virtual byte[] Data { get; set; }

        [Parameter("bytes32[]", "factoryDeps", 12)]
        public virtual List<byte[]> FactoryDeps { get; set; }

        [Parameter("bytes", "paymasterInput", 13)]
        public virtual byte[] PaymasterInput { get; set; }
    }

    public class ZkPaymasterDataResponse
    {
        [JsonProperty("paymaster")]
        public string Paymaster { get; set; }

        [JsonProperty("paymasterInput")]
        public string PaymasterInput { get; set; }
    }

    public class ZkBroadcastTransactionResponse
    {
        [JsonProperty("transactionHash")]
        public string TransactionHash { get; set; }
    }

    [Struct("InitializerInstallModule")]
    public class InitializerInstallModule
    {
        [Parameter("uint256", "moduleTypeId", 1)]
        public virtual BigInteger ModuleTypeId { get; set; }

        [Parameter("address", "module", 2)]
        public virtual string Module { get; set; }

        [Parameter("bytes", "initData", 3)]
        public virtual byte[] InitData { get; set; }
    }
}
