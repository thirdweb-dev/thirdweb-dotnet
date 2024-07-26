using Thirdweb;
using dotenv.net;
using System.Diagnostics;
using Thirdweb.Pay;
using Newtonsoft.Json;
using Nethereum.Hex.HexTypes;
using System.Numerics;

DotEnv.Load();

// Do not use secret keys client side, use client id/bundle id instead
var secretKey = Environment.GetEnvironmentVariable("THIRDWEB_SECRET_KEY");

// Do not use private keys client side, use InAppWallet/SmartWallet instead
var privateKey = Environment.GetEnvironmentVariable("PRIVATE_KEY");

// Fetch timeout options are optional, default is 60000ms
var client = ThirdwebClient.Create(secretKey: secretKey, fetchTimeoutOptions: new TimeoutOptions(storage: 30000, rpc: 60000));

var contract = await ThirdwebContract.Create(client: client, address: "0x81ebd23aA79bCcF5AaFb9c9c5B0Db4223c39102e", chain: 421614);
var readResult = await contract.ERC20_Name();
Console.WriteLine($"Contract read result: {readResult}");

// Create wallets (this is an advanced use case, typically one wallet is plenty)
var privateKeyWallet = await PrivateKeyWallet.Create(client: client, privateKeyHex: privateKey);

// var privateKeyWallet = await PrivateKeyWallet.Generate(client: client);
var walletAddress = await privateKeyWallet.GetAddress();
Console.WriteLine($"Private Key Wallet address: {walletAddress}");

var smartWallet7579 = await SmartWallet.Create(
    personalWallet: privateKeyWallet,
    chainId: 11155111,
    gasless: true,
    factoryAddress: "0x296e57E8a808aE80625A3CB5Ba95684fb99fEff2",
    entryPoint: Thirdweb.Constants.ENTRYPOINT_ADDRESS_V07
);
Console.WriteLine($"Smart Wallet address: {await smartWallet7579.GetAddress()}");

var receipt = await smartWallet7579.SendTransaction(
    new ThirdwebTransactionInput()
    {
        From = await smartWallet7579.GetAddress(),
        To = await smartWallet7579.GetAddress(),
        Value = new HexBigInteger(BigInteger.Zero),
        Data = "0x"
    }
);
Console.WriteLine($"Transaction receipt: {JsonConvert.SerializeObject(receipt, Formatting.Indented)}");

// var chainData = await Utils.FetchThirdwebChainDataAsync(client, 421614);
// Console.WriteLine($"Chain data: {JsonConvert.SerializeObject(chainData, Formatting.Indented)}");

// var smartWallet = await SmartWallet.Create(privateKeyWallet, 78600);

// // self transfer 0
// var tx = await ThirdwebTransaction.Create(
//     smartWallet,
//     new ThirdwebTransactionInput()
//     {
//         From = await smartWallet.GetAddress(),
//         To = await smartWallet.GetAddress(),
//         Value = new HexBigInteger(BigInteger.Zero)
//     },
//     78600
// );
// var txHash = await ThirdwebTransaction.Send(tx);
// Console.WriteLine($"Transaction hash: {txHash}");

// // Buy with Fiat
// // Find out more about supported FIAT currencies
// var supportedCurrencies = await ThirdwebPay.GetBuyWithFiatCurrencies(client);
// Console.WriteLine($"Supported currencies: {JsonConvert.SerializeObject(supportedCurrencies, Formatting.Indented)}");

// // Get a Buy with Fiat quote
// var fiatQuoteParams = new BuyWithFiatQuoteParams(
//     fromCurrencySymbol: "USD",
//     toAddress: walletAddress,
//     toChainId: "137",
//     toTokenAddress: Thirdweb.Constants.NATIVE_TOKEN_ADDRESS,
//     toAmount: "20"
// );
// var fiatOnrampQuote = await ThirdwebPay.GetBuyWithFiatQuote(client, fiatQuoteParams);
// Console.WriteLine($"Fiat onramp quote: {JsonConvert.SerializeObject(fiatOnrampQuote, Formatting.Indented)}");

// // Get a Buy with Fiat link
// var onRampLink = ThirdwebPay.BuyWithFiat(fiatOnrampQuote);
// Console.WriteLine($"Fiat onramp link: {onRampLink}");

// // Open onramp link to start the process (use your framework's version of this)
// var psi = new ProcessStartInfo { FileName = onRampLink, UseShellExecute = true };
// _ = Process.Start(psi);

// // Poll for status
// var currentOnRampStatus = OnRampStatus.NONE;
// while (currentOnRampStatus is not OnRampStatus.ON_RAMP_TRANSFER_COMPLETED and not OnRampStatus.ON_RAMP_TRANSFER_FAILED)
// {
//     var onRampStatus = await ThirdwebPay.GetBuyWithFiatStatus(client, fiatOnrampQuote.IntentId);
//     currentOnRampStatus = Enum.Parse<OnRampStatus>(onRampStatus.Status);
//     Console.WriteLine($"Fiat onramp status: {JsonConvert.SerializeObject(onRampStatus, Formatting.Indented)}");
//     await Task.Delay(5000);
// }

// Buy with Crypto

// Swap Polygon MATIC to Base ETH
// var swapQuoteParams = new BuyWithCryptoQuoteParams(
//     fromAddress: walletAddress,
//     fromChainId: 137,
//     fromTokenAddress: Thirdweb.Constants.NATIVE_TOKEN_ADDRESS,
//     toTokenAddress: Thirdweb.Constants.NATIVE_TOKEN_ADDRESS,
//     toChainId: 8453,
//     toAmount: "0.1"
// );
// var swapQuote = await ThirdwebPay.GetBuyWithCryptoQuote(client, swapQuoteParams);
// Console.WriteLine($"Swap quote: {JsonConvert.SerializeObject(swapQuote, Formatting.Indented)}");

// // Initiate swap
// var txHash = await ThirdwebPay.BuyWithCrypto(wallet: privateKeyWallet, buyWithCryptoQuote: swapQuote);
// Console.WriteLine($"Swap transaction hash: {txHash}");

// // Poll for status
// var currentSwapStatus = SwapStatus.NONE;
// while (currentSwapStatus is not SwapStatus.COMPLETED and not SwapStatus.FAILED)
// {
//     var swapStatus = await ThirdwebPay.GetBuyWithCryptoStatus(client, txHash);
//     currentSwapStatus = Enum.Parse<SwapStatus>(swapStatus.Status);
//     Console.WriteLine($"Swap status: {JsonConvert.SerializeObject(swapStatus, Formatting.Indented)}");
//     await Task.Delay(5000);
// }


// var inAppWallet = await InAppWallet.Create(client: client, email: "firekeeper+awsless@thirdweb.com"); // or email: null, phoneNumber: "+1234567890"

// var inAppWallet = await InAppWallet.Create(client: client, authprovider: AuthProvider.Google); // or email: null, phoneNumber: "+1234567890"

// // Reset InAppWallet (optional step for testing login flow)
// if (await inAppWallet.IsConnected())
// {
//     await inAppWallet.Disconnect();
// }

// // Relog if InAppWallet not logged in
// if (!await inAppWallet.IsConnected())
// {
//     var address = await inAppWallet.LoginWithOauth(
//         isMobile: false,
//         (url) =>
//         {
//             var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
//             _ = Process.Start(psi);
//         },
//         "thirdweb://",
//         new InAppWalletBrowser()
//     );
//     Console.WriteLine($"InAppWallet address: {address}");
// }

// await inAppWallet.SendOTP();
// Console.WriteLine("Please submit the OTP.");
// retry:
// var otp = Console.ReadLine();
// (var inAppWalletAddress, var canRetry) = await inAppWallet.SubmitOTP(otp);
// if (inAppWalletAddress == null && canRetry)
// {
//     Console.WriteLine("Please submit the OTP again.");
//     goto retry;
// }
// if (inAppWalletAddress == null)
// {
//     Console.WriteLine("OTP login failed. Please try again.");
//     return;
// }
// Console.WriteLine($"InAppWallet address: {inAppWalletAddress}");
// }

// Prepare a transaction directly, or with Contract.Prepare
// var tx = await ThirdwebTransaction.Create(
//     client: client,
//     wallet: privateKeyWallet,
//     txInput: new ThirdwebTransactionInput()
//     {
//         From = await privateKeyWallet.GetAddress(),
//         To = await privateKeyWallet.GetAddress(),
//         Value = new HexBigInteger(BigInteger.Zero),
//     },
//     chainId: 300
// );

// // Set zkSync options
// tx.SetZkSyncOptions(
//     new ZkSyncOptions(
//         // Paymaster contract address
//         paymaster: "0xbA226d47Cbb2731CBAA67C916c57d68484AA269F",
//         // IPaymasterFlow interface encoded data
//         paymasterInput: "0x8c5a344500000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000000"
//     )
// );

// // Send as usual, it's now gasless!
// var txHash = await ThirdwebTransaction.Send(transaction: tx);
// Console.WriteLine($"Transaction hash: {txHash}");

// var zkSmartWallet = await SmartWallet.Create(client: client, personalWallet: privateKeyWallet, chainId: 302, gasless: true);
// Console.WriteLine($"Smart wallet address: {await zkSmartWallet.GetAddress()}");
// var zkAaTx = await ThirdwebTransaction.Create(client, zkSmartWallet, new ThirdwebTransactionInput() { From = await zkSmartWallet.GetAddress(), To = await zkSmartWallet.GetAddress(), }, 302);
// var zkSyncSignatureBasedAaTxHash = await ThirdwebTransaction.Send(zkAaTx);
// Console.WriteLine($"Transaction hash: {zkSyncSignatureBasedAaTxHash}");

// Create smart wallet with InAppWallet signer
// var smartWallet = await SmartWallet.Create(client: client, personalWallet: inAppWallet, factoryAddress: "0xbf1C9aA4B1A085f7DA890a44E82B0A1289A40052", gasless: true, chainId: 421614);
// var res = await smartWallet.Authenticate("http://localhost:8000", 421614);
// Console.WriteLine($"Smart wallet auth result: {res}");

// // Grant a session key to pk wallet (advanced use case)
// _ = await smartWallet.CreateSessionKey(
//     signerAddress: await privateKeyWallet.GetAddress(),
//     approvedTargets: new List<string>() { Constants.ADDRESS_ZERO },
//     nativeTokenLimitPerTransactionInWei: "0",
//     permissionStartTimestamp: "0",
//     permissionEndTimestamp: (Utils.GetUnixTimeStampNow() + 86400).ToString(),
//     reqValidityStartTimestamp: "0",
//     reqValidityEndTimestamp: Utils.GetUnixTimeStampIn10Years().ToString()
// );

// // Reconnect to same smart wallet with pk wallet as signer (specifying wallet address override)
// smartWallet = await SmartWallet.Create(
//     client: client,
//     personalWallet: privateKeyWallet,
//     factoryAddress: "0xbf1C9aA4B1A085f7DA890a44E82B0A1289A40052",
//     gasless: true,
//     chainId: 421614,
//     accountAddressOverride: await smartWallet.GetAddress()
// );

// // Log addresses
// Console.WriteLine($"PrivateKey Wallet: {await privateKeyWallet.GetAddress()}");
// Console.WriteLine($"InAppWallet: {await inAppWallet.GetAddress()}");
// Console.WriteLine($"Smart Wallet: {await smartWallet.GetAddress()}");

// // Sign, triggering deploy as needed and 1271 verification if it's a smart wallet
// var message = "Hello, Thirdweb!";
// var signature = await smartWallet.PersonalSign(message);
// Console.WriteLine($"Signed message: {signature}");

// var balanceBefore = await ThirdwebContract.Read<BigInteger>(contract, "balanceOf", await smartWallet.GetAddress());
// Console.WriteLine($"Balance before mint: {balanceBefore}");

// var writeResult = await ThirdwebContract.Write(smartWallet, contract, "mintTo", 0, await smartWallet.GetAddress(), 100);
// Console.WriteLine($"Contract write result: {writeResult}");

// var balanceAfter = await ThirdwebContract.Read<BigInteger>(contract, "balanceOf", await smartWallet.GetAddress());
// Console.WriteLine($"Balance after mint: {balanceAfter}");

// // Transaction Builder
// var preparedTx = await ThirdwebContract.Prepare(wallet: smartWallet, contract: contract, method: "mintTo", weiValue: 0, parameters: new object[] { await smartWallet.GetAddress(), 100 });
// Console.WriteLine($"Prepared transaction: {preparedTx}");
// var estimatedCosts = await ThirdwebTransaction.EstimateGasCosts(preparedTx);
// Console.WriteLine($"Estimated ETH gas cost: {estimatedCosts.ether}");
// var totalCosts = await ThirdwebTransaction.EstimateTotalCosts(preparedTx);
// Console.WriteLine($"Estimated ETH total cost: {totalCosts.ether}");
// var simulationData = await ThirdwebTransaction.Simulate(preparedTx);
// Console.WriteLine($"Simulation data: {simulationData}");
// var txHash = await ThirdwebTransaction.Send(preparedTx);
// Console.WriteLine($"Transaction hash: {txHash}");
// var receipt = await ThirdwebTransaction.WaitForTransactionReceipt(client, 421614, txHash);
// Console.WriteLine($"Transaction receipt: {JsonConvert.SerializeObject(receipt)}");

// // Transaction Builder - raw transfer
// var rawTx = new ThirdwebTransactionInput
// {
//     From = await smartWallet.GetAddress(),
//     To = await smartWallet.GetAddress(),
//     Value = new HexBigInteger(BigInteger.Zero),
//     Data = "0x",
// };
// var preparedRawTx = await ThirdwebTransaction.Create(client: client, wallet: smartWallet, txInput: rawTx, chainId: 421614);
// Console.WriteLine($"Prepared raw transaction: {preparedRawTx}");
// var estimatedCostsRaw = await ThirdwebTransaction.EstimateGasCosts(preparedRawTx);
// Console.WriteLine($"Estimated ETH gas cost: {estimatedCostsRaw.ether}");
// var totalCostsRaw = await ThirdwebTransaction.EstimateTotalCosts(preparedRawTx);
// Console.WriteLine($"Estimated ETH total cost: {totalCostsRaw.ether}");
// var simulationDataRaw = await ThirdwebTransaction.Simulate(preparedRawTx);
// Console.WriteLine($"Simulation data: {simulationDataRaw}");
// var txHashRaw = await ThirdwebTransaction.Send(preparedRawTx);
// Console.WriteLine($"Raw transaction hash: {txHashRaw}");
// var receiptRaw = await ThirdwebTransaction.WaitForTransactionReceipt(client, 421614, txHashRaw);
// Console.WriteLine($"Raw transaction receipt: {JsonConvert.SerializeObject(receiptRaw)}");


// Storage actions

// // Will download from IPFS or normal urls
// var downloadResult = await ThirdwebStorage.Download<string>(client: client, uri: "AnyUrlIncludingIpfs");
// Console.WriteLine($"Download result: {downloadResult}");

// // Will upload to IPFS
// var uploadResult = await ThirdwebStorage.Upload(client: client, path: "AnyPath");
// Console.WriteLine($"Upload result preview: {uploadResult.PreviewUrl}");


// Access RPC directly if needed, generally not recommended

// var rpc = ThirdwebRPC.GetRpcInstance(client, 421614);
// var blockNumber = await rpc.SendRequestAsync<string>("eth_blockNumber");
// Console.WriteLine($"Block number: {blockNumber}");
