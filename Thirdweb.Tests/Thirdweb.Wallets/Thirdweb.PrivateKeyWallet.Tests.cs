﻿using Nethereum.Hex.HexTypes;

namespace Thirdweb.Tests.Wallets;

public class PrivateKeyWalletTests : BaseTests
{
    public PrivateKeyWalletTests(ITestOutputHelper output)
        : base(output) { }

    private async Task<PrivateKeyWallet> GetAccount()
    {
        var privateKeyAccount = await PrivateKeyWallet.Generate(this.Client);
        return privateKeyAccount;
    }

    [Fact(Timeout = 120000)]
    public async Task Initialization_Success()
    {
        var account = await this.GetAccount();
        Assert.NotNull(account);
    }

    [Fact(Timeout = 120000)]
    public async void Create_NullClient()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => PrivateKeyWallet.Create(null, "0x1234567890abcdef"));
    }

    [Fact(Timeout = 120000)]
    public async void Create_NullPrivateKey()
    {
        var client = this.Client;
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await PrivateKeyWallet.Create(client, null));
        Assert.Equal("Private key cannot be null or empty. (Parameter 'privateKeyHex')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async void Create_EmptyPrivateKey()
    {
        var client = this.Client;
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await PrivateKeyWallet.Create(client, string.Empty));
        Assert.Equal("Private key cannot be null or empty. (Parameter 'privateKeyHex')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async void Generate_NullClient()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => PrivateKeyWallet.Generate(null));
    }

    [Fact(Timeout = 120000)]
    public async void LoadOrGenerate_NullClient()
    {
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => PrivateKeyWallet.LoadOrGenerate(null));
    }

    [Fact(Timeout = 120000)]
    public async void SaveAndDelete_CheckPath()
    {
        var wallet = await PrivateKeyWallet.Generate(this.Client);
        await wallet.Save();

        var path = PrivateKeyWallet.GetSavePath();
        Assert.True(File.Exists(path));

        PrivateKeyWallet.Delete();
        Assert.False(File.Exists(path));
    }

    [Fact(Timeout = 120000)]
    public async Task Connect()
    {
        var account = await this.GetAccount();
        Assert.True(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task GetAddress()
    {
        var account = await this.GetAccount();
        var address = await account.GetAddress();
        Assert.True(address.Length == 42);
    }

    [Fact(Timeout = 120000)]
    public async Task EthSign_Success()
    {
        var account = await this.GetAccount();
        var message = "Hello, World!";
        var signature = await account.EthSign(message);
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task EthSign_NullMessage()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.EthSign(null as string));
        Assert.Equal("Message to sign cannot be null. (Parameter 'message')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task EthSignRaw_Success()
    {
        var account = await this.GetAccount();
        var message = "Hello, World!";
        var signature = await account.EthSign(System.Text.Encoding.UTF8.GetBytes(message));
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task EthSignRaw_NullMessage()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.EthSign(null as byte[]));
        Assert.Equal("Message to sign cannot be null. (Parameter 'rawMessage')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSign_Success()
    {
        var account = await this.GetAccount();
        var message = "Hello, World!";
        var signature = await account.PersonalSign(message);
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSign_EmptyMessage()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.PersonalSign(string.Empty));
        Assert.Equal("Message to sign cannot be null. (Parameter 'message')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSign_NullyMessage()
    {
        var account = await this.GetAccount();

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.PersonalSign(null as string));
        Assert.Equal("Message to sign cannot be null. (Parameter 'message')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSignRaw_Success()
    {
        var account = await this.GetAccount();
        var message = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
        var signature = await account.PersonalSign(message);
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task PersonalSignRaw_NullMessage()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.PersonalSign(null as byte[]));
        Assert.Equal("Message to sign cannot be null. (Parameter 'rawMessage')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_Success()
    {
        var account = await this.GetAccount();
        var json =
            /*lang=json,strict*/
            "{\"types\":{\"EIP712Domain\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"version\",\"type\":\"string\"},{\"name\":\"chainId\",\"type\":\"uint256\"},{\"name\":\"verifyingContract\",\"type\":\"address\"}],\"Person\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"wallet\",\"type\":\"address\"}],\"Mail\":[{\"name\":\"from\",\"type\":\"Person\"},{\"name\":\"to\",\"type\":\"Person\"},{\"name\":\"contents\",\"type\":\"string\"}]},\"primaryType\":\"Mail\",\"domain\":{\"name\":\"Ether Mail\",\"version\":\"1\",\"chainId\":1,\"verifyingContract\":\"0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC\"},\"message\":{\"from\":{\"name\":\"Cow\",\"wallet\":\"0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826\"},\"to\":{\"name\":\"Bob\",\"wallet\":\"0xbBbBBBBbbBBBbbbBbbBbbBBbBbbBbBbBbBbbBBbB\"},\"contents\":\"Hello, Bob!\"}}";
        var signature = await account.SignTypedDataV4(json);
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_NullJson()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.SignTypedDataV4(null));
        Assert.Equal("Json to sign cannot be null. (Parameter 'json')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_EmptyJson()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.SignTypedDataV4(string.Empty));
        Assert.Equal("Json to sign cannot be null. (Parameter 'json')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_Typed()
    {
        var account = await this.GetAccount();
        var typedData = EIP712.GetTypedDefinition_SmartAccount_AccountMessage("Account", "1", 421614, await account.GetAddress());
        var accountMessage = new AccountAbstraction.AccountMessage { Message = System.Text.Encoding.UTF8.GetBytes("Hello, world!") };
        var signature = await account.SignTypedDataV4(accountMessage, typedData);
        Assert.True(signature.Length == 132);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTypedDataV4_Typed_NullData()
    {
        var account = await this.GetAccount();
        var typedData = EIP712.GetTypedDefinition_SmartAccount_AccountMessage("Account", "1", 421614, await account.GetAddress());
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.SignTypedDataV4(null as string, typedData));
        Assert.Equal("Data to sign cannot be null. (Parameter 'data')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_Success()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            // Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            // Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            GasPrice = new HexBigInteger(10000000000),
        };
        var signature = await account.SignTransaction(transaction);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_NoFrom_Success()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            To = Constants.ADDRESS_ZERO,
            // Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            GasPrice = new HexBigInteger(10000000000),
        };
        var signature = await account.SignTransaction(transaction);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_NullTransaction()
    {
        var account = await this.GetAccount();
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.SignTransaction(null));
        Assert.Equal("Value cannot be null. (Parameter 'transaction')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_NoNonce()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x"
        };
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => account.SignTransaction(transaction));
        Assert.Equal("Transaction nonce has not been set (Parameter 'transaction')", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_NoGasPrice()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            ChainId = new HexBigInteger(421614)
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => account.SignTransaction(transaction));
        Assert.Equal("Transaction MaxPriorityFeePerGas and MaxFeePerGas must be set for EIP-1559 transactions", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_1559_Success()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            MaxFeePerGas = new HexBigInteger(10000000000),
            MaxPriorityFeePerGas = new HexBigInteger(10000000000),
        };
        var signature = await account.SignTransaction(transaction);
        Assert.NotNull(signature);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_1559_NoMaxFeePerGas()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            MaxPriorityFeePerGas = new HexBigInteger(10000000000),
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => account.SignTransaction(transaction));
        Assert.Equal("Transaction MaxPriorityFeePerGas and MaxFeePerGas must be set for EIP-1559 transactions", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task SignTransaction_1559_NoMaxPriorityFeePerGas()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Gas = new HexBigInteger(21000),
            Data = "0x",
            Nonce = new HexBigInteger(99999999999),
            MaxFeePerGas = new HexBigInteger(10000000000),
        };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => account.SignTransaction(transaction));
        Assert.Equal("Transaction MaxPriorityFeePerGas and MaxFeePerGas must be set for EIP-1559 transactions", ex.Message);
    }

    [Fact(Timeout = 120000)]
    public async Task IsConnected_True()
    {
        var account = await this.GetAccount();
        Assert.True(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task IsConnected_False()
    {
        var account = await this.GetAccount();
        await account.Disconnect();
        Assert.False(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task Disconnect()
    {
        var account = await this.GetAccount();
        await account.Disconnect();
        Assert.False(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task Disconnect_NotConnected()
    {
        var account = await this.GetAccount();
        await account.Disconnect();
        Assert.False(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task Disconnect_Connected()
    {
        var account = await this.GetAccount();
        await account.Disconnect();
        Assert.False(await account.IsConnected());
    }

    [Fact(Timeout = 120000)]
    public async Task SendTransaction_InvalidOperation()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Data = "0x",
        };
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => account.SendTransaction(transaction));
    }

    [Fact(Timeout = 120000)]
    public async Task ExecuteTransaction_InvalidOperation()
    {
        var account = await this.GetAccount();
        var transaction = new ThirdwebTransactionInput(421614)
        {
            From = await account.GetAddress(),
            To = Constants.ADDRESS_ZERO,
            Value = new HexBigInteger(0),
            Data = "0x",
        };
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => account.ExecuteTransaction(transaction));
    }

    [Fact(Timeout = 120000)]
    public async Task LoadOrGenerate_LoadsExistingWallet()
    {
        // Generate and save a wallet to simulate an existing wallet file
        var wallet = await PrivateKeyWallet.Generate(this.Client);
        await wallet.Save();

        var loadedWallet = await PrivateKeyWallet.LoadOrGenerate(this.Client);

        Assert.NotNull(loadedWallet);
        Assert.Equal(await wallet.Export(), await loadedWallet.Export());
        Assert.Equal(await wallet.GetAddress(), await loadedWallet.GetAddress());

        // Clean up
        var path = PrivateKeyWallet.GetSavePath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    [Fact(Timeout = 120000)]
    public async Task LoadOrGenerate_GeneratesNewWalletIfNoExistingWallet()
    {
        var path = PrivateKeyWallet.GetSavePath();

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var wallet = await PrivateKeyWallet.LoadOrGenerate(this.Client);

        Assert.NotNull(wallet);
        Assert.NotNull(await wallet.Export());
        Assert.False(File.Exists(path));
    }

    [Fact(Timeout = 120000)]
    public async Task Save_SavesPrivateKeyToFile()
    {
        var wallet = await PrivateKeyWallet.Generate(this.Client);

        await wallet.Save();

        var path = PrivateKeyWallet.GetSavePath();
        Assert.True(File.Exists(path));

        var savedPrivateKey = await File.ReadAllTextAsync(path);
        Assert.Equal(await wallet.Export(), savedPrivateKey);

        // Clean up
        File.Delete(path);
    }

    [Fact(Timeout = 120000)]
    public async Task Export_ReturnsPrivateKey()
    {
        var wallet = await PrivateKeyWallet.Generate(this.Client);

        var privateKey = await wallet.Export();

        Assert.NotNull(privateKey);
        Assert.Equal(privateKey, await wallet.Export());
    }
}
