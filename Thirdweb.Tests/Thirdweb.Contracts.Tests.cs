using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Thirdweb.Tests;

public class ContractsTests : BaseTests
{
    public ContractsTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task FetchAbi()
    {
        var abi = await ThirdwebContract.FetchAbi(address: "0x1320Cafa93fb53Ed9068E3272cb270adbBEf149C", chainId: 84532);
        Assert.NotNull(abi);
        Assert.NotEmpty(abi);
    }

    [Fact]
    public void InitTest_NullClient()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ThirdwebContract(null, "0x123", 1, "[]"));
        Assert.Contains("Client must be provided", exception.Message);
    }

    [Fact]
    public void InitTest_NullAddress()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ThirdwebContract(new ThirdwebClient(secretKey: _secretKey), null, 1, "[]"));
        Assert.Contains("Address must be provided", exception.Message);
    }

    [Fact]
    public void InitTest_ZeroChain()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ThirdwebContract(new ThirdwebClient(secretKey: _secretKey), "0x123", 0, "[]"));
        Assert.Contains("Chain must be provided", exception.Message);
    }

    [Fact]
    public void InitTest_NullAbi()
    {
        var exception = Assert.Throws<ArgumentException>(() => new ThirdwebContract(new ThirdwebClient(secretKey: _secretKey), "0x123", 1, null));
        Assert.Contains("Abi must be provided", exception.Message);
    }

    [Fact]
    public async Task ReadTest_String()
    {
        var contract = GetContract();
        var result = await ThirdwebContract.ReadContract<string>(contract, "name");
        Assert.Equal("Kitty DropERC20", result);
    }

    [Fact]
    public async Task ReadTest_BigInteger()
    {
        var contract = GetContract();
        var result = await ThirdwebContract.ReadContract<BigInteger>(contract, "decimals");
        Assert.Equal(18, result);
    }

    [Nethereum.ABI.FunctionEncoding.Attributes.FunctionOutput]
    private class GetPlatformFeeInfoOutputDTO : Nethereum.ABI.FunctionEncoding.Attributes.IFunctionOutputDTO
    {
        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("address", "", 1)]
        public virtual required string ReturnValue1 { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("uint16", "", 2)]
        public virtual required ushort ReturnValue2 { get; set; }
    }

    [Fact]
    public async Task ReadTest_Tuple()
    {
        var contract = GetContract();
        var result = await ThirdwebContract.ReadContract<GetPlatformFeeInfoOutputDTO>(contract, "getPlatformFeeInfo");
        Assert.Equal("0xDaaBDaaC8073A7dAbdC96F6909E8476ab4001B34", result.ReturnValue1);
        Assert.Equal(0, result.ReturnValue2);
    }

    private class AllowlistProof
    {
        public List<byte[]> Proof { get; set; } = new();
        public BigInteger QuantityLimitPerWallet { get; set; } = BigInteger.Zero;
        public BigInteger PricePerToken { get; set; } = BigInteger.Zero;
        public string Currency { get; set; } = Constants.ADDRESS_ZERO;
    }

    [Fact]
    public async Task WriteTest_SmartAccount()
    {
        var contract = GetContract();
        var wallet = await GetWallet();
        var receiver = await wallet.GetAddress();
        var quantity = BigInteger.One;
        var currency = Constants.NATIVE_TOKEN_ADDRESS;
        var pricePerToken = BigInteger.Zero;
        var allowlistProof = new object[] { new byte[] { }, BigInteger.Zero, BigInteger.Zero, Constants.ADDRESS_ZERO };
        var data = new byte[] { };
        var result = await ThirdwebContract.WriteContract(wallet, contract, "claim", 0, receiver, quantity, currency, pricePerToken, allowlistProof, data);
        Assert.NotNull(result);
        var receipt = await Utils.GetTransactionReceipt(contract.Client, contract.Chain, result);
        Assert.NotNull(receipt);
        Assert.Equal(result, receipt.TransactionHash);
    }

    [Fact]
    public async Task WriteTest_PrivateKeyAccount()
    {
        var contract = GetContract();
        var privateKeyAccount = new PrivateKeyAccount(contract.Client, _testPrivateKey);
        await privateKeyAccount.Connect();
        var wallet = new ThirdwebWallet();
        await wallet.Initialize(new List<IThirdwebAccount> { privateKeyAccount });
        var receiver = await wallet.GetAddress();
        var quantity = BigInteger.One;
        var currency = Constants.NATIVE_TOKEN_ADDRESS;
        var pricePerToken = BigInteger.Zero;
        var allowlistProof = new object[] { new byte[] { }, BigInteger.Zero, BigInteger.Zero, Constants.ADDRESS_ZERO };
        var data = new byte[] { };
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await ThirdwebContract.WriteContract(wallet, contract, "claim", 0, receiver, quantity, currency, pricePerToken, allowlistProof, data)
        );
        Assert.Contains("insufficient funds", exception.Message);
    }

    private async Task<ThirdwebWallet> GetWallet()
    {
        var client = new ThirdwebClient(secretKey: _secretKey);
        var privateKeyAccount = new PrivateKeyAccount(client, _testPrivateKey);
        var smartAccount = new SmartAccount(client, personalAccount: privateKeyAccount, factoryAddress: "0xbf1C9aA4B1A085f7DA890a44E82B0A1289A40052", gasless: true, chainId: 421614);
        await privateKeyAccount.Connect();
        await smartAccount.Connect();
        var wallet = new ThirdwebWallet();
        await wallet.Initialize(new List<IThirdwebAccount> { privateKeyAccount, smartAccount });
        wallet.SetActive(await smartAccount.GetAddress());
        return wallet;
    }

    private ThirdwebContract GetContract()
    {
        var client = new ThirdwebClient(secretKey: _secretKey);
        var contract = new ThirdwebContract(
            client: client,
            address: "0xEBB8a39D865465F289fa349A67B3391d8f910da9",
            chain: 421614,
            abi: "[{\"type\": \"constructor\",\"name\": \"\",\"inputs\": [],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"event\",\"name\": \"Approval\",\"inputs\": [{\"type\": \"address\",\"name\": \"owner\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"spender\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"value\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"ClaimConditionsUpdated\",\"inputs\": [{\"type\": \"tuple[]\",\"name\": \"claimConditions\",\"components\": [{\"type\": \"uint256\",\"name\": \"startTimestamp\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"maxClaimableSupply\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"supplyClaimed\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"quantityLimitPerWallet\",\"internalType\": \"uint256\"},{\"type\": \"bytes32\",\"name\": \"merkleRoot\",\"internalType\": \"bytes32\"},{\"type\": \"uint256\",\"name\": \"pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"currency\",\"internalType\": \"address\"},{\"type\": \"string\",\"name\": \"metadata\",\"internalType\": \"string\"}],\"indexed\": false,\"internalType\": \"struct IClaimCondition.ClaimCondition[]\"},{\"type\": \"bool\",\"name\": \"resetEligibility\",\"indexed\": false,\"internalType\": \"bool\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"ContractURIUpdated\",\"inputs\": [{\"type\": \"string\",\"name\": \"prevURI\",\"indexed\": false,\"internalType\": \"string\"},{\"type\": \"string\",\"name\": \"newURI\",\"indexed\": false,\"internalType\": \"string\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"DelegateChanged\",\"inputs\": [{\"type\": \"address\",\"name\": \"delegator\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"fromDelegate\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"toDelegate\",\"indexed\": true,\"internalType\": \"address\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"DelegateVotesChanged\",\"inputs\": [{\"type\": \"address\",\"name\": \"delegate\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"previousBalance\",\"indexed\": false,\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"newBalance\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"EIP712DomainChanged\",\"inputs\": [],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"FlatPlatformFeeUpdated\",\"inputs\": [{\"type\": \"address\",\"name\": \"platformFeeRecipient\",\"indexed\": false,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"flatFee\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"Initialized\",\"inputs\": [{\"type\": \"uint8\",\"name\": \"version\",\"indexed\": false,\"internalType\": \"uint8\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"MaxTotalSupplyUpdated\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"maxTotalSupply\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"PlatformFeeInfoUpdated\",\"inputs\": [{\"type\": \"address\",\"name\": \"platformFeeRecipient\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"platformFeeBps\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"PlatformFeeTypeUpdated\",\"inputs\": [{\"type\": \"uint8\",\"name\": \"feeType\",\"indexed\": false,\"internalType\": \"enum IPlatformFee.PlatformFeeType\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"PrimarySaleRecipientUpdated\",\"inputs\": [{\"type\": \"address\",\"name\": \"recipient\",\"indexed\": true,\"internalType\": \"address\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"RoleAdminChanged\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"indexed\": true,\"internalType\": \"bytes32\"},{\"type\": \"bytes32\",\"name\": \"previousAdminRole\",\"indexed\": true,\"internalType\": \"bytes32\"},{\"type\": \"bytes32\",\"name\": \"newAdminRole\",\"indexed\": true,\"internalType\": \"bytes32\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"RoleGranted\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"indexed\": true,\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"sender\",\"indexed\": true,\"internalType\": \"address\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"RoleRevoked\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"indexed\": true,\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"sender\",\"indexed\": true,\"internalType\": \"address\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"TokensClaimed\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"claimConditionIndex\",\"indexed\": true,\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"claimer\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"receiver\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"startTokenId\",\"indexed\": false,\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"quantityClaimed\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"event\",\"name\": \"Transfer\",\"inputs\": [{\"type\": \"address\",\"name\": \"from\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"to\",\"indexed\": true,\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"value\",\"indexed\": false,\"internalType\": \"uint256\"}],\"outputs\": [],\"anonymous\": false},{\"type\": \"function\",\"name\": \"CLOCK_MODE\",\"inputs\": [],\"outputs\": [{\"type\": \"string\",\"name\": \"\",\"internalType\": \"string\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"DEFAULT_ADMIN_ROLE\",\"inputs\": [],\"outputs\": [{\"type\": \"bytes32\",\"name\": \"\",\"internalType\": \"bytes32\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"DOMAIN_SEPARATOR\",\"inputs\": [],\"outputs\": [{\"type\": \"bytes32\",\"name\": \"\",\"internalType\": \"bytes32\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"allowance\",\"inputs\": [{\"type\": \"address\",\"name\": \"owner\",\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"spender\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"approve\",\"inputs\": [{\"type\": \"address\",\"name\": \"spender\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"amount\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"balanceOf\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"burn\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"amount\",\"internalType\": \"uint256\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"burnFrom\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"amount\",\"internalType\": \"uint256\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"checkpoints\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"},{\"type\": \"uint32\",\"name\": \"pos\",\"internalType\": \"uint32\"}],\"outputs\": [{\"type\": \"tuple\",\"name\": \"\",\"components\": [{\"type\": \"uint32\",\"name\": \"fromBlock\",\"internalType\": \"uint32\"},{\"type\": \"uint224\",\"name\": \"votes\",\"internalType\": \"uint224\"}],\"internalType\": \"struct ERC20VotesUpgradeable.Checkpoint\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"claim\",\"inputs\": [{\"type\": \"address\",\"name\": \"_receiver\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_quantity\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"_currency\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"tuple\",\"name\": \"_allowlistProof\",\"components\": [{\"type\": \"bytes32[]\",\"name\": \"proof\",\"internalType\": \"bytes32[]\"},{\"type\": \"uint256\",\"name\": \"quantityLimitPerWallet\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"currency\",\"internalType\": \"address\"}],\"internalType\": \"struct IDrop.AllowlistProof\"},{\"type\": \"bytes\",\"name\": \"_data\",\"internalType\": \"bytes\"}],\"outputs\": [],\"stateMutability\": \"payable\"},{\"type\": \"function\",\"name\": \"claimCondition\",\"inputs\": [],\"outputs\": [{\"type\": \"uint256\",\"name\": \"currentStartId\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"count\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"clock\",\"inputs\": [],\"outputs\": [{\"type\": \"uint48\",\"name\": \"\",\"internalType\": \"uint48\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"contractType\",\"inputs\": [],\"outputs\": [{\"type\": \"bytes32\",\"name\": \"\",\"internalType\": \"bytes32\"}],\"stateMutability\": \"pure\"},{\"type\": \"function\",\"name\": \"contractURI\",\"inputs\": [],\"outputs\": [{\"type\": \"string\",\"name\": \"\",\"internalType\": \"string\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"contractVersion\",\"inputs\": [],\"outputs\": [{\"type\": \"uint8\",\"name\": \"\",\"internalType\": \"uint8\"}],\"stateMutability\": \"pure\"},{\"type\": \"function\",\"name\": \"decimals\",\"inputs\": [],\"outputs\": [{\"type\": \"uint8\",\"name\": \"\",\"internalType\": \"uint8\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"decreaseAllowance\",\"inputs\": [{\"type\": \"address\",\"name\": \"spender\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"subtractedValue\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"delegate\",\"inputs\": [{\"type\": \"address\",\"name\": \"delegatee\",\"internalType\": \"address\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"delegateBySig\",\"inputs\": [{\"type\": \"address\",\"name\": \"delegatee\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"nonce\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"expiry\",\"internalType\": \"uint256\"},{\"type\": \"uint8\",\"name\": \"v\",\"internalType\": \"uint8\"},{\"type\": \"bytes32\",\"name\": \"r\",\"internalType\": \"bytes32\"},{\"type\": \"bytes32\",\"name\": \"s\",\"internalType\": \"bytes32\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"delegates\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"address\",\"name\": \"\",\"internalType\": \"address\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"eip712Domain\",\"inputs\": [],\"outputs\": [{\"type\": \"bytes1\",\"name\": \"fields\",\"internalType\": \"bytes1\"},{\"type\": \"string\",\"name\": \"name\",\"internalType\": \"string\"},{\"type\": \"string\",\"name\": \"version\",\"internalType\": \"string\"},{\"type\": \"uint256\",\"name\": \"chainId\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"verifyingContract\",\"internalType\": \"address\"},{\"type\": \"bytes32\",\"name\": \"salt\",\"internalType\": \"bytes32\"},{\"type\": \"uint256[]\",\"name\": \"extensions\",\"internalType\": \"uint256[]\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getActiveClaimConditionId\",\"inputs\": [],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getClaimConditionById\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"_conditionId\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"tuple\",\"name\": \"condition\",\"components\": [{\"type\": \"uint256\",\"name\": \"startTimestamp\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"maxClaimableSupply\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"supplyClaimed\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"quantityLimitPerWallet\",\"internalType\": \"uint256\"},{\"type\": \"bytes32\",\"name\": \"merkleRoot\",\"internalType\": \"bytes32\"},{\"type\": \"uint256\",\"name\": \"pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"currency\",\"internalType\": \"address\"},{\"type\": \"string\",\"name\": \"metadata\",\"internalType\": \"string\"}],\"internalType\": \"struct IClaimCondition.ClaimCondition\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getFlatPlatformFeeInfo\",\"inputs\": [],\"outputs\": [{\"type\": \"address\",\"name\": \"\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getPastTotalSupply\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"timepoint\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getPastVotes\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"timepoint\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getPlatformFeeInfo\",\"inputs\": [],\"outputs\": [{\"type\": \"address\",\"name\": \"\",\"internalType\": \"address\"},{\"type\": \"uint16\",\"name\": \"\",\"internalType\": \"uint16\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getPlatformFeeType\",\"inputs\": [],\"outputs\": [{\"type\": \"uint8\",\"name\": \"\",\"internalType\": \"enum IPlatformFee.PlatformFeeType\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getRoleAdmin\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"}],\"outputs\": [{\"type\": \"bytes32\",\"name\": \"\",\"internalType\": \"bytes32\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getRoleMember\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"uint256\",\"name\": \"index\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"address\",\"name\": \"member\",\"internalType\": \"address\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getRoleMemberCount\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"count\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getSupplyClaimedByWallet\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"_conditionId\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"_claimer\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"supplyClaimedByWallet\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"getVotes\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"grantRole\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"hasRole\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"hasRoleWithSwitch\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"increaseAllowance\",\"inputs\": [{\"type\": \"address\",\"name\": \"spender\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"addedValue\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"initialize\",\"inputs\": [{\"type\": \"address\",\"name\": \"_defaultAdmin\",\"internalType\": \"address\"},{\"type\": \"string\",\"name\": \"_name\",\"internalType\": \"string\"},{\"type\": \"string\",\"name\": \"_symbol\",\"internalType\": \"string\"},{\"type\": \"string\",\"name\": \"_contractURI\",\"internalType\": \"string\"},{\"type\": \"address[]\",\"name\": \"_trustedForwarders\",\"internalType\": \"address[]\"},{\"type\": \"address\",\"name\": \"_saleRecipient\",\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"_platformFeeRecipient\",\"internalType\": \"address\"},{\"type\": \"uint128\",\"name\": \"_platformFeeBps\",\"internalType\": \"uint128\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"isTrustedForwarder\",\"inputs\": [{\"type\": \"address\",\"name\": \"forwarder\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"maxTotalSupply\",\"inputs\": [],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"multicall\",\"inputs\": [{\"type\": \"bytes[]\",\"name\": \"data\",\"internalType\": \"bytes[]\"}],\"outputs\": [{\"type\": \"bytes[]\",\"name\": \"results\",\"internalType\": \"bytes[]\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"name\",\"inputs\": [],\"outputs\": [{\"type\": \"string\",\"name\": \"\",\"internalType\": \"string\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"nonces\",\"inputs\": [{\"type\": \"address\",\"name\": \"owner\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"numCheckpoints\",\"inputs\": [{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [{\"type\": \"uint32\",\"name\": \"\",\"internalType\": \"uint32\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"permit\",\"inputs\": [{\"type\": \"address\",\"name\": \"owner\",\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"spender\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"value\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"deadline\",\"internalType\": \"uint256\"},{\"type\": \"uint8\",\"name\": \"v\",\"internalType\": \"uint8\"},{\"type\": \"bytes32\",\"name\": \"r\",\"internalType\": \"bytes32\"},{\"type\": \"bytes32\",\"name\": \"s\",\"internalType\": \"bytes32\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"primarySaleRecipient\",\"inputs\": [],\"outputs\": [{\"type\": \"address\",\"name\": \"\",\"internalType\": \"address\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"renounceRole\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"revokeRole\",\"inputs\": [{\"type\": \"bytes32\",\"name\": \"role\",\"internalType\": \"bytes32\"},{\"type\": \"address\",\"name\": \"account\",\"internalType\": \"address\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setClaimConditions\",\"inputs\": [{\"type\": \"tuple[]\",\"name\": \"_conditions\",\"components\": [{\"type\": \"uint256\",\"name\": \"startTimestamp\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"maxClaimableSupply\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"supplyClaimed\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"quantityLimitPerWallet\",\"internalType\": \"uint256\"},{\"type\": \"bytes32\",\"name\": \"merkleRoot\",\"internalType\": \"bytes32\"},{\"type\": \"uint256\",\"name\": \"pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"currency\",\"internalType\": \"address\"},{\"type\": \"string\",\"name\": \"metadata\",\"internalType\": \"string\"}],\"internalType\": \"struct IClaimCondition.ClaimCondition[]\"},{\"type\": \"bool\",\"name\": \"_resetClaimEligibility\",\"internalType\": \"bool\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setContractURI\",\"inputs\": [{\"type\": \"string\",\"name\": \"_uri\",\"internalType\": \"string\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setFlatPlatformFeeInfo\",\"inputs\": [{\"type\": \"address\",\"name\": \"_platformFeeRecipient\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_flatFee\",\"internalType\": \"uint256\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setMaxTotalSupply\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"_maxTotalSupply\",\"internalType\": \"uint256\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setPlatformFeeInfo\",\"inputs\": [{\"type\": \"address\",\"name\": \"_platformFeeRecipient\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_platformFeeBps\",\"internalType\": \"uint256\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setPlatformFeeType\",\"inputs\": [{\"type\": \"uint8\",\"name\": \"_feeType\",\"internalType\": \"enum IPlatformFee.PlatformFeeType\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"setPrimarySaleRecipient\",\"inputs\": [{\"type\": \"address\",\"name\": \"_saleRecipient\",\"internalType\": \"address\"}],\"outputs\": [],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"symbol\",\"inputs\": [],\"outputs\": [{\"type\": \"string\",\"name\": \"\",\"internalType\": \"string\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"totalSupply\",\"inputs\": [],\"outputs\": [{\"type\": \"uint256\",\"name\": \"\",\"internalType\": \"uint256\"}],\"stateMutability\": \"view\"},{\"type\": \"function\",\"name\": \"transfer\",\"inputs\": [{\"type\": \"address\",\"name\": \"to\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"amount\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"transferFrom\",\"inputs\": [{\"type\": \"address\",\"name\": \"from\",\"internalType\": \"address\"},{\"type\": \"address\",\"name\": \"to\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"amount\",\"internalType\": \"uint256\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"\",\"internalType\": \"bool\"}],\"stateMutability\": \"nonpayable\"},{\"type\": \"function\",\"name\": \"verifyClaim\",\"inputs\": [{\"type\": \"uint256\",\"name\": \"_conditionId\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"_claimer\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_quantity\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"_currency\",\"internalType\": \"address\"},{\"type\": \"uint256\",\"name\": \"_pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"tuple\",\"name\": \"_allowlistProof\",\"components\": [{\"type\": \"bytes32[]\",\"name\": \"proof\",\"internalType\": \"bytes32[]\"},{\"type\": \"uint256\",\"name\": \"quantityLimitPerWallet\",\"internalType\": \"uint256\"},{\"type\": \"uint256\",\"name\": \"pricePerToken\",\"internalType\": \"uint256\"},{\"type\": \"address\",\"name\": \"currency\",\"internalType\": \"address\"}],\"internalType\": \"struct IDrop.AllowlistProof\"}],\"outputs\": [{\"type\": \"bool\",\"name\": \"isOverride\",\"internalType\": \"bool\"}],\"stateMutability\": \"view\"}]"
        );
        return contract;
    }
}
