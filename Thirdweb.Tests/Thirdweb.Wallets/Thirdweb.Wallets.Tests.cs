﻿using Nethereum.Hex.HexTypes;

namespace Thirdweb.Tests.Wallets;

public class WalletTests : BaseTests
{
    public WalletTests(ITestOutputHelper output)
        : base(output) { }

    private async Task<SmartWallet> GetAccount()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var privateKeyAccount = await PrivateKeyWallet.Generate(client);
        var smartAccount = await SmartWallet.Create(personalWallet: privateKeyAccount, factoryAddress: "0xbf1C9aA4B1A085f7DA890a44E82B0A1289A40052", gasless: true, chainId: 421614);
        return smartAccount;
    }

    [Fact(Timeout = 120000)]
    public async Task GetAddress()
    {
        var wallet = await GetAccount();
        Assert.Equal(await wallet.GetAddress(), await wallet.GetAddress());
    }

    [Fact(Timeout = 120000)]
    public async Task EthSignRaw()
    {
        var wallet = await GetAccount();
        var message = "Hello, world!";
        var signature = await wallet.EthSign(System.Text.Encoding.UTF8.GetBytes(message));
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task EthSign()
    {
        var wallet = await GetAccount();
        var message = "Hello, world!";
        var signature = await wallet.EthSign(message);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSignRaw()
    {
        var wallet = await GetAccount();
        var message = "Hello, world!";
        var signature = await wallet.PersonalSign(System.Text.Encoding.UTF8.GetBytes(message));
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSign()
    {
        var wallet = await GetAccount();
        var message = "Hello, world!";
        var signature = await wallet.PersonalSign(message);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4()
    {
        var wallet = await GetAccount();
        var json =
            "{\"types\":{\"EIP712Domain\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"version\",\"type\":\"string\"},{\"name\":\"chainId\",\"type\":\"uint256\"},{\"name\":\"verifyingContract\",\"type\":\"address\"}],\"Person\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"wallet\",\"type\":\"address\"}],\"Mail\":[{\"name\":\"from\",\"type\":\"Person\"},{\"name\":\"to\",\"type\":\"Person\"},{\"name\":\"contents\",\"type\":\"string\"}]},\"primaryType\":\"Mail\",\"domain\":{\"name\":\"Ether Mail\",\"version\":\"1\",\"chainId\":1,\"verifyingContract\":\"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\"},\"message\":{\"from\":{\"name\":\"Cow\",\"wallet\":\"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\"},\"to\":{\"name\":\"Bob\",\"wallet\":\"0xbBbBBBBbbBBBbbbBbbBbbBBbBbbBbBbBbBbbBBbB\"},\"contents\":\"Hello, Bob!\"}}";
        var signature = await wallet.SignTypedDataV4(json);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_Typed()
    {
        var wallet = await GetAccount();
        var typedData = EIP712.GetTypedDefinition_SmartAccount_AccountMessage("Account", "1", 421614, await wallet.GetAddress());
        var accountMessage = new AccountAbstraction.AccountMessage { Message = System.Text.Encoding.UTF8.GetBytes("Hello, world!").HashPrefixedMessage() };
        var signature = await wallet.SignTypedDataV4(accountMessage, typedData);
        Assert.NotNull(signature);

        var signerAcc = await (wallet).GetPersonalAccount();
        var gen1 = await EIP712.GenerateSignature_SmartAccount_AccountMessage(
            "Account",
            "1",
            421614,
            await wallet.GetAddress(),
            System.Text.Encoding.UTF8.GetBytes("Hello, world!").HashPrefixedMessage(),
            signerAcc
        );
        Assert.Equal(gen1, signature);

        var req = new AccountAbstraction.SignerPermissionRequest()
        {
            Signer = await wallet.GetAddress(),
            IsAdmin = 0,
            ApprovedTargets = new List<string>() { Constants.ADDRESS_ZERO },
            NativeTokenLimitPerTransaction = 0,
            PermissionStartTimestamp = 0,
            ReqValidityStartTimestamp = 0,
            PermissionEndTimestamp = 0,
            Uid = new byte[32]
        };

        var typedData2 = EIP712.GetTypedDefinition_SmartAccount("Account", "1", 421614, await wallet.GetAddress());
        var signature2 = await wallet.SignTypedDataV4(req, typedData2);
        Assert.NotNull(signature2);

        var gen2 = await EIP712.GenerateSignature_SmartAccount("Account", "1", 421614, await wallet.GetAddress(), req, signerAcc);
        Assert.Equal(gen2, signature2);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction()
    {
        var wallet = await GetAccount();
        var transaction = new ThirdwebTransactionInput
        {
            To = await wallet.GetAddress(),
            Data = "0x",
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            GasPrice = new HexBigInteger(10000000000),
            Nonce = new HexBigInteger(9999999999999),
            ChainId = new HexBigInteger(421614),
        };
        var rpc = ThirdwebRPC.GetRpcInstance(ThirdwebClient.Create(secretKey: _secretKey), 421614);
        var signature = await wallet.SignTransaction(transaction);
        Assert.NotNull(signature);
    }
}
