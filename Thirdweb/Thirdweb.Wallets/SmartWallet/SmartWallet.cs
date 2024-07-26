using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Nethereum.ABI.EIP712;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using Newtonsoft.Json;
using Thirdweb.AccountAbstraction;

namespace Thirdweb
{
    public class SmartWallet : IThirdwebWallet
    {
        public ThirdwebClient Client { get; }

        public ThirdwebAccountType AccountType => ThirdwebAccountType.SmartAccount;

        public bool IsDeploying { get; private set; }

        private IThirdwebWallet _personalAccount;
        private bool _gasless;
        private ThirdwebContract _factoryContract;
        private ThirdwebContract _accountContract;
        private ThirdwebContract _entryPointContract;
        private BigInteger _chainId;
        private string _bundlerUrl;
        private string _paymasterUrl;

        protected SmartWallet(
            IThirdwebWallet personalAccount,
            bool gasless,
            BigInteger chainId,
            string bundlerUrl,
            string paymasterUrl,
            ThirdwebContract entryPointContract,
            ThirdwebContract factoryContract,
            ThirdwebContract accountContract
        )
        {
            Client = personalAccount.Client;

            _personalAccount = personalAccount;
            _gasless = gasless;
            _chainId = chainId;
            _bundlerUrl = bundlerUrl;
            _paymasterUrl = paymasterUrl;
            _entryPointContract = entryPointContract;
            _factoryContract = factoryContract;
            _accountContract = accountContract;
        }

        public static async Task<SmartWallet> Create(
            IThirdwebWallet personalWallet,
            BigInteger chainId,
            bool gasless = true,
            string factoryAddress = null,
            string accountAddressOverride = null,
            string entryPoint = null,
            string bundlerUrl = null,
            string paymasterUrl = null
        )
        {
            if (!await personalWallet.IsConnected())
            {
                throw new InvalidOperationException("SmartAccount.Connect: Personal account must be connected.");
            }

            entryPoint ??= Constants.ENTRYPOINT_ADDRESS_V06;

            var entryPointVersion = Utils.GetEntryPointVersion(entryPoint);

            bundlerUrl ??= entryPointVersion == 6 ? $"https://{chainId}.bundler.thirdweb.com" : $"https://{chainId}.bundler.thirdweb.com/v2";
            paymasterUrl ??= entryPointVersion == 6 ? $"https://{chainId}.bundler.thirdweb.com" : $"https://{chainId}.bundler.thirdweb.com/v2";
            factoryAddress ??= Constants.DEFAULT_FACTORY_ADDRESS;

            ThirdwebContract entryPointContract = null;
            ThirdwebContract factoryContract = null;
            ThirdwebContract accountContract = null;

            if (!Utils.IsZkSync(chainId))
            {
                var entryPointAbi = entryPointVersion == 6 ? Constants.ENTRYPOINT_V06_ABI : Constants.ENTRYPOINT_V07_ABI;
                var factoryAbi = entryPointVersion == 6 ? Constants.FACTORY_V06_ABI : Constants.FACTORY_V07_ABI;
                var accountAbi = entryPointVersion == 6 ? Constants.ACCOUNT_V06_ABI : Constants.ACCOUNT_V07_ABI;

                entryPointContract = await ThirdwebContract.Create(personalWallet.Client, entryPoint, chainId, entryPointAbi);
                factoryContract = await ThirdwebContract.Create(personalWallet.Client, factoryAddress, chainId, factoryAbi);

                var personalAddress = await personalWallet.GetAddress();
                var accountAddress = accountAddressOverride ?? await ThirdwebContract.Read<string>(factoryContract, "getAddress", personalAddress, new byte[0]);

                accountContract = await ThirdwebContract.Create(personalWallet.Client, accountAddress, chainId, accountAbi);
            }

            return new SmartWallet(personalWallet, gasless, chainId, bundlerUrl, paymasterUrl, entryPointContract, factoryContract, accountContract);
        }

        public async Task<bool> IsDeployed()
        {
            if (Utils.IsZkSync(_chainId))
            {
                return true;
            }

            var code = await ThirdwebRPC.GetRpcInstance(Client, _chainId).SendRequestAsync<string>("eth_getCode", _accountContract.Address, "latest");
            return code != "0x";
        }

        public async Task<string> SendTransaction(ThirdwebTransactionInput transactionInput)
        {
            if (transactionInput == null)
            {
                throw new InvalidOperationException("SmartAccount.SendTransaction: Transaction input is required.");
            }

            if (Utils.IsZkSync(_chainId))
            {
                var transaction = await ThirdwebTransaction.Create(_personalAccount, transactionInput, _chainId);

                if (transactionInput.Nonce == null)
                {
                    _ = transaction.SetNonce(await ThirdwebTransaction.GetNonce(transaction));
                }
                if (transactionInput.Gas == null)
                {
                    _ = transaction.SetGasLimit(await ThirdwebTransaction.EstimateGasLimit(transaction));
                }
                if (transactionInput.MaxFeePerGas == null)
                {
                    (var maxFee, _) = await ThirdwebTransaction.EstimateGasFees(transaction);
                    _ = transaction.SetMaxFeePerGas(maxFee);
                }

                if (_gasless)
                {
                    (var paymaster, var paymasterInput) = await ZkPaymasterData(transactionInput);
                    transaction = transaction.SetZkSyncOptions(new ZkSyncOptions(paymaster: paymaster, paymasterInput: paymasterInput));
                    var zkTx = await ThirdwebTransaction.ConvertToZkSyncTransaction(transaction);
                    var zkTxSigned = await EIP712.GenerateSignature_ZkSyncTransaction("zkSync", "2", transaction.Input.ChainId.Value, zkTx, this);
                    // Match bundler ZkTransactionInput type without recreating
                    var hash = await ZkBroadcastTransaction(
                        new
                        {
                            nonce = zkTx.Nonce.ToString(),
                            from = zkTx.From,
                            to = zkTx.To,
                            gas = zkTx.GasLimit.ToString(),
                            gasPrice = string.Empty,
                            value = zkTx.Value.ToString(),
                            data = Utils.BytesToHex(zkTx.Data),
                            maxFeePerGas = zkTx.MaxFeePerGas.ToString(),
                            maxPriorityFeePerGas = zkTx.MaxPriorityFeePerGas.ToString(),
                            chainId = _chainId.ToString(),
                            signedTransaction = zkTxSigned,
                            paymaster = paymaster
                        }
                    );
                    return hash;
                }
                else
                {
                    return await ThirdwebTransaction.Send(transaction);
                }
            }
            else
            {
                var signedOp = await SignUserOp(transactionInput);
                return await SendUserOp(signedOp);
            }
        }

        private async Task<(byte[] initCode, string factory, string factoryData)> GetInitCode()
        {
            if (await IsDeployed())
            {
                return (new byte[] { }, null, null);
            }

            var entryPointVersion = Utils.GetEntryPointVersion(_entryPointContract.Address);
            var personalAccountAddress = await _personalAccount.GetAddress();
            var factoryContract = new Contract(null, _factoryContract.Abi, _factoryContract.Address);
            var createFunction = factoryContract.GetFunction("createAccount");
            var data =
                entryPointVersion == 6
                    ? createFunction.GetData(personalAccountAddress, new byte { })
                    : createFunction.GetData(personalAccountAddress, new byte { }, Array.Empty<InitializerInstallModule>());
            return (Utils.HexConcat(_factoryContract.Address, data).HexToBytes(), _factoryContract.Address, data);
        }

        private async Task<object> SignUserOp(ThirdwebTransactionInput transactionInput, int? requestId = null, bool simulation = false)
        {
            requestId ??= 1;

            (var initCode, var factory, var factoryData) = await GetInitCode();

            // Wait until deployed to avoid double initCode
            if (!simulation)
            {
                if (IsDeploying)
                {
                    initCode = new byte[] { };
                }

                while (IsDeploying)
                {
                    await Task.Delay(1000); // Wait for the deployment to finish
                }

                IsDeploying = initCode.Length > 0;
            }

            // Create the user operation and its safe (hexified) version

            var fees = await BundlerClient.ThirdwebGetUserOperationGasPrice(Client, _bundlerUrl, requestId);
            var maxFee = new HexBigInteger(fees.MaxFeePerGas).Value;
            var maxPriorityFee = new HexBigInteger(fees.MaxPriorityFeePerGas).Value;

            var entryPointVersion = Utils.GetEntryPointVersion(_entryPointContract.Address);

            if (entryPointVersion == 6)
            {
                var executeFn = new ExecuteFunctionV6
                {
                    Target = transactionInput.To,
                    Value = transactionInput.Value.Value,
                    Calldata = transactionInput.Data.HexToBytes(),
                    FromAddress = await GetAddress(),
                };
                var executeInput = executeFn.CreateTransactionInput(await GetAddress());

                var partialUserOp = new UserOperationV6()
                {
                    Sender = _accountContract.Address,
                    Nonce = await GetNonce(),
                    InitCode = initCode,
                    CallData = executeInput.Data.HexToBytes(),
                    CallGasLimit = 0,
                    VerificationGasLimit = 0,
                    PreVerificationGas = 0,
                    MaxFeePerGas = maxFee,
                    MaxPriorityFeePerGas = maxPriorityFee,
                    PaymasterAndData = new byte[] { },
                    Signature = Constants.DUMMY_SIG.HexToBytes(),
                };

                // Update paymaster data if any

                partialUserOp.PaymasterAndData = (await GetPaymasterAndData(requestId, EncodeUserOperation(partialUserOp))).PaymasterAndData.HexToBytes();

                // Estimate gas

                var gasEstimates = await BundlerClient.EthEstimateUserOperationGas(Client, _bundlerUrl, requestId, EncodeUserOperation(partialUserOp), _entryPointContract.Address);
                partialUserOp.CallGasLimit = 50000 + new HexBigInteger(gasEstimates.CallGasLimit).Value;
                partialUserOp.VerificationGasLimit = new HexBigInteger(gasEstimates.VerificationGasLimit).Value;
                partialUserOp.PreVerificationGas = new HexBigInteger(gasEstimates.PreVerificationGas).Value;

                // Update paymaster data if any

                partialUserOp.PaymasterAndData = (await GetPaymasterAndData(requestId, EncodeUserOperation(partialUserOp))).PaymasterAndData.HexToBytes();

                // Hash, sign and encode the user operation

                partialUserOp.Signature = await HashAndSignUserOp(partialUserOp, _entryPointContract);

                return partialUserOp;
            }
            else
            {
                var executeFn = new ExecuteFunctionV7
                {
                    Mode = ModeLib.EncodeSimpleSingle().Value,
                    ExecutionCalldata = ExecutionLib.EncodeSingle(transactionInput.To.HexToBytes(), transactionInput.Value.HexValue.HexToBytes32(), transactionInput.Data.HexToBytes())
                };
                var executeInput = executeFn.CreateTransactionInput(await GetAddress());

                var partialUserOp = new UserOperationV7()
                {
                    Sender = _accountContract.Address,
                    Nonce = await GetNonce(),
                    Factory = factory,
                    FactoryData = factoryData.HexToBytes(),
                    CallData = executeInput.Data.HexToBytes(),
                    CallGasLimit = 0,
                    VerificationGasLimit = 0,
                    PreVerificationGas = 0,
                    MaxFeePerGas = maxFee,
                    MaxPriorityFeePerGas = maxPriorityFee,
                    Paymaster = null,
                    PaymasterVerificationGasLimit = 0,
                    PaymasterPostOpGasLimit = 0,
                    PaymasterData = new byte[] { },
                    Signature = Constants.DUMMY_SIG.HexToBytes(),
                };

                // Update paymaster data if any

                var res = await GetPaymasterAndData(requestId, EncodeUserOperation(partialUserOp));
                partialUserOp.Paymaster = res.Paymaster;
                partialUserOp.PaymasterData = res.PaymasterData?.HexToBytes() ?? new byte[] { };
                partialUserOp.PreVerificationGas = new HexBigInteger(res.PreVerificationGas ?? "0").Value;
                partialUserOp.VerificationGasLimit = new HexBigInteger(res.VerificationGasLimit ?? "0").Value;
                partialUserOp.CallGasLimit = new HexBigInteger(res.CallGasLimit ?? "0").Value;
                partialUserOp.PaymasterVerificationGasLimit = new HexBigInteger(res.PaymasterVerificationGasLimit ?? "0").Value;
                partialUserOp.PaymasterPostOpGasLimit = new HexBigInteger(res.PaymasterPostOpGasLimit ?? "0").Value;

                // Estimate gas

                if (
                    partialUserOp.PreVerificationGas.IsZero
                    || partialUserOp.VerificationGasLimit.IsZero
                    || partialUserOp.CallGasLimit.IsZero
                    || partialUserOp.PaymasterVerificationGasLimit.IsZero
                    || partialUserOp.PaymasterPostOpGasLimit.IsZero
                )
                {
                    var gasEstimates = await BundlerClient.EthEstimateUserOperationGas(Client, _bundlerUrl, requestId, EncodeUserOperation(partialUserOp), _entryPointContract.Address);
                    partialUserOp.CallGasLimit = 21000 + new HexBigInteger(gasEstimates.CallGasLimit).Value;
                    partialUserOp.VerificationGasLimit = new HexBigInteger(gasEstimates.VerificationGasLimit).Value;
                    partialUserOp.PreVerificationGas = new HexBigInteger(gasEstimates.PreVerificationGas).Value;
                    partialUserOp.PaymasterVerificationGasLimit = new HexBigInteger(gasEstimates.PaymasterVerificationGasLimit).Value;
                    partialUserOp.PaymasterPostOpGasLimit = new HexBigInteger(gasEstimates.PaymasterPostOpGasLimit).Value;

                    // Update paymaster data if any

                    res = await GetPaymasterAndData(requestId, EncodeUserOperation(partialUserOp));
                    partialUserOp.Paymaster = res.Paymaster;
                    partialUserOp.PaymasterData = res.PaymasterData.HexToBytes();
                }

                // Hash, sign and encode the user operation

                partialUserOp.Signature = await HashAndSignUserOp(partialUserOp, _entryPointContract);

                return partialUserOp;
            }
        }

        private async Task<string> SendUserOp(object userOperation, int? requestId = null)
        {
            requestId ??= 1;

            // Encode op

            object encodedOp;
            if (userOperation is UserOperationV6)
            {
                encodedOp = EncodeUserOperation(userOperation as UserOperationV6);
            }
            else if (userOperation is UserOperationV7)
            {
                encodedOp = EncodeUserOperation(userOperation as UserOperationV7);
            }
            else
            {
                throw new Exception("Invalid signed operation type");
            }

            // Send the user operation

            var userOpHash = await BundlerClient.EthSendUserOperation(Client, _bundlerUrl, requestId, encodedOp, _entryPointContract.Address);

            // Wait for the transaction to be mined

            string txHash = null;
            while (txHash == null)
            {
                var userOpReceipt = await BundlerClient.EthGetUserOperationReceipt(Client, _bundlerUrl, requestId, userOpHash);
                txHash = userOpReceipt?.Receipt?.TransactionHash;
                await Task.Delay(1000).ConfigureAwait(false);
            }

            IsDeploying = false;
            return txHash;
        }

        private async Task<BigInteger> GetNonce()
        {
            var randomBytes = new byte[24];
            RandomNumberGenerator.Fill(randomBytes);
            BigInteger randomInt192 = new(randomBytes);
            randomInt192 = BigInteger.Abs(randomInt192) % (BigInteger.One << 192);
            return await ThirdwebContract.Read<BigInteger>(_entryPointContract, "getNonce", await GetAddress(), randomInt192);
        }

        private async Task<(string, string)> ZkPaymasterData(ThirdwebTransactionInput transactionInput)
        {
            if (_gasless)
            {
                var result = await BundlerClient.ZkPaymasterData(Client, _paymasterUrl, 1, transactionInput);
                return (result.Paymaster, result.PaymasterInput);
            }
            else
            {
                return (null, null);
            }
        }

        private async Task<string> ZkBroadcastTransaction(object transactionInput)
        {
            var result = await BundlerClient.ZkBroadcastTransaction(Client, _bundlerUrl, 1, transactionInput);
            return result.TransactionHash;
        }

        private async Task<PMSponsorOperationResponse> GetPaymasterAndData(object requestId, object userOp)
        {
            return _gasless ? await BundlerClient.PMSponsorUserOperation(Client, _paymasterUrl, requestId, userOp, _entryPointContract.Address) : new PMSponsorOperationResponse();
        }

        private async Task<byte[]> HashAndSignUserOp(UserOperationV6 userOp, ThirdwebContract entryPointContract)
        {
            var userOpHash = await ThirdwebContract.Read<byte[]>(entryPointContract, "getUserOpHash", userOp);
            var sig = await _personalAccount.PersonalSign(userOpHash);
            return sig.HexToBytes();
        }

        private async Task<byte[]> HashAndSignUserOp(UserOperationV7 userOp, ThirdwebContract entryPointContract)
        {
            var factoryBytes = userOp.Factory.HexToBytes();
            var factoryDataBytes = userOp.FactoryData;
            var initCodeBuffer = new byte[factoryBytes.Length + factoryDataBytes.Length];
            Buffer.BlockCopy(factoryBytes, 0, initCodeBuffer, 0, factoryBytes.Length);
            Buffer.BlockCopy(factoryDataBytes, 0, initCodeBuffer, factoryBytes.Length, factoryDataBytes.Length);

            var verificationGasLimitBytes = userOp.VerificationGasLimit.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var callGasLimitBytes = userOp.CallGasLimit.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var accountGasLimitsBuffer = new byte[32];
            Buffer.BlockCopy(verificationGasLimitBytes, 0, accountGasLimitsBuffer, 0, 16);
            Buffer.BlockCopy(callGasLimitBytes, 0, accountGasLimitsBuffer, 16, 16);

            var maxPriorityFeePerGasBytes = userOp.MaxPriorityFeePerGas.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var maxFeePerGasBytes = userOp.MaxFeePerGas.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var gasFeesBuffer = new byte[32];
            Buffer.BlockCopy(maxPriorityFeePerGasBytes, 0, gasFeesBuffer, 0, 16);
            Buffer.BlockCopy(maxFeePerGasBytes, 0, gasFeesBuffer, 16, 16);

            var paymasterBytes = userOp.Paymaster.HexToBytes();
            var paymasterVerificationGasLimitBytes = userOp.PaymasterVerificationGasLimit.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var paymasterPostOpGasLimitBytes = userOp.PaymasterPostOpGasLimit.ToHexBigInteger().HexValue.HexToBytes().PadBytes(16);
            var paymasterDataBytes = userOp.PaymasterData;
            var paymasterAndDataBuffer = new byte[20 + 16 + 16 + paymasterDataBytes.Length];
            Buffer.BlockCopy(paymasterBytes, 0, paymasterAndDataBuffer, 0, 20);
            Buffer.BlockCopy(paymasterVerificationGasLimitBytes, 0, paymasterAndDataBuffer, 20, 16);
            Buffer.BlockCopy(paymasterPostOpGasLimitBytes, 0, paymasterAndDataBuffer, 20 + 16, 16);
            Buffer.BlockCopy(paymasterDataBytes, 0, paymasterAndDataBuffer, 20 + 16 + 16, paymasterDataBytes.Length);

            var packedOp = new PackedUserOperation()
            {
                Sender = userOp.Sender,
                Nonce = userOp.Nonce,
                InitCode = initCodeBuffer,
                CallData = userOp.CallData,
                AccountGasLimits = accountGasLimitsBuffer,
                PreVerificationGas = userOp.PreVerificationGas,
                GasFees = gasFeesBuffer,
                PaymasterAndData = paymasterAndDataBuffer,
                Signature = userOp.Signature
            };

            var userOpHash = await ThirdwebContract.Read<byte[]>(entryPointContract, "getUserOpHash", packedOp);

            var sig = await _personalAccount.PersonalSign(userOpHash);

            return sig.HexToBytes();
        }

        private UserOperationHexifiedV6 EncodeUserOperation(UserOperationV6 userOperation)
        {
            return new UserOperationHexifiedV6()
            {
                sender = userOperation.Sender,
                nonce = userOperation.Nonce.ToHexBigInteger().HexValue,
                initCode = userOperation.InitCode.BytesToHex(),
                callData = userOperation.CallData.BytesToHex(),
                callGasLimit = userOperation.CallGasLimit.ToHexBigInteger().HexValue,
                verificationGasLimit = userOperation.VerificationGasLimit.ToHexBigInteger().HexValue,
                preVerificationGas = userOperation.PreVerificationGas.ToHexBigInteger().HexValue,
                maxFeePerGas = userOperation.MaxFeePerGas.ToHexBigInteger().HexValue,
                maxPriorityFeePerGas = userOperation.MaxPriorityFeePerGas.ToHexBigInteger().HexValue,
                paymasterAndData = userOperation.PaymasterAndData.BytesToHex(),
                signature = userOperation.Signature.BytesToHex()
            };
        }

        private UserOperationHexifiedV7 EncodeUserOperation(UserOperationV7 userOperation)
        {
            return new UserOperationHexifiedV7()
            {
                sender = userOperation.Sender,
                nonce = Utils.HexConcat(Constants.ADDRESS_ZERO, userOperation.Nonce.ToHexBigInteger().HexValue),
                factory = userOperation.Factory,
                factoryData = userOperation.FactoryData.BytesToHex(),
                callData = userOperation.CallData.BytesToHex(),
                callGasLimit = userOperation.CallGasLimit.ToHexBigInteger().HexValue,
                verificationGasLimit = userOperation.VerificationGasLimit.ToHexBigInteger().HexValue,
                preVerificationGas = userOperation.PreVerificationGas.ToHexBigInteger().HexValue,
                maxFeePerGas = userOperation.MaxFeePerGas.ToHexBigInteger().HexValue,
                maxPriorityFeePerGas = userOperation.MaxPriorityFeePerGas.ToHexBigInteger().HexValue,
                paymaster = userOperation.Paymaster,
                paymasterVerificationGasLimit = userOperation.PaymasterVerificationGasLimit.ToHexBigInteger().HexValue,
                paymasterPostOpGasLimit = userOperation.PaymasterPostOpGasLimit.ToHexBigInteger().HexValue,
                paymasterData = userOperation.PaymasterData.BytesToHex(),
                signature = userOperation.Signature.BytesToHex()
            };
        }

        public async Task ForceDeploy()
        {
            if (Utils.IsZkSync(_chainId))
            {
                return;
            }

            if (await IsDeployed())
            {
                return;
            }

            if (IsDeploying)
            {
                throw new InvalidOperationException("SmartAccount.ForceDeploy: Account is already deploying.");
            }

            var input = new ThirdwebTransactionInput()
            {
                Data = "0x",
                To = _accountContract.Address,
                Value = new HexBigInteger(0)
            };
            var txHash = await SendTransaction(input);
            _ = await ThirdwebTransaction.WaitForTransactionReceipt(Client, _chainId, txHash);
        }

        public Task<IThirdwebWallet> GetPersonalAccount()
        {
            return Task.FromResult(_personalAccount);
        }

        public async Task<string> GetAddress()
        {
            return Utils.IsZkSync(_chainId) ? await _personalAccount.GetAddress() : _accountContract.Address.ToChecksumAddress();
        }

        public Task<string> EthSign(byte[] rawMessage)
        {
            return _personalAccount.EthSign(rawMessage);
        }

        public Task<string> EthSign(string message)
        {
            return _personalAccount.EthSign(message);
        }

        public Task<string> RecoverAddressFromEthSign(string message, string signature)
        {
            return _personalAccount.RecoverAddressFromEthSign(message, signature);
        }

        public Task<string> PersonalSign(byte[] rawMessage)
        {
            return _personalAccount.PersonalSign(rawMessage);
        }

        public async Task<string> PersonalSign(string message)
        {
            if (Utils.IsZkSync(_chainId))
            {
                return await _personalAccount.PersonalSign(message);
            }

            if (!await IsDeployed())
            {
                while (IsDeploying)
                {
                    await Task.Delay(1000); // Wait for the deployment to finish
                }
                await ForceDeploy();
            }

            if (await IsDeployed())
            {
                var originalMsgHash = System.Text.Encoding.UTF8.GetBytes(message).HashPrefixedMessage();
                bool factorySupports712;
                try
                {
                    _ = await ThirdwebContract.Read<byte[]>(_accountContract, "getMessageHash", originalMsgHash);
                    factorySupports712 = true;
                }
                catch
                {
                    factorySupports712 = false;
                }

                var sig = factorySupports712
                    ? await EIP712.GenerateSignature_SmartAccount_AccountMessage("Account", "1", _chainId, await GetAddress(), originalMsgHash, _personalAccount)
                    : await _personalAccount.PersonalSign(originalMsgHash);

                var isValid = await IsValidSignature(message, sig);
                return isValid ? sig : throw new Exception("Invalid signature.");
            }
            else
            {
                throw new Exception("Smart account could not be deployed, unable to sign message.");
            }
        }

        public async Task<string> RecoverAddressFromPersonalSign(string message, string signature)
        {
            if (!await IsValidSignature(message, signature))
            {
                return await _personalAccount.RecoverAddressFromPersonalSign(message, signature);
            }
            else
            {
                return await GetAddress();
            }
        }

        public async Task<bool> IsValidSignature(string message, string signature)
        {
            try
            {
                var magicValue = await ThirdwebContract.Read<byte[]>(_accountContract, "isValidSignature", message.HashPrefixedMessage().HexToBytes(), signature.HexToBytes());
                return magicValue.BytesToHex() == new byte[] { 0x16, 0x26, 0xba, 0x7e }.BytesToHex();
            }
            catch
            {
                return false;
            }
        }

        public async Task<ThirdwebTransactionReceipt> CreateSessionKey(
            string signerAddress,
            List<string> approvedTargets,
            string nativeTokenLimitPerTransactionInWei,
            string permissionStartTimestamp,
            string permissionEndTimestamp,
            string reqValidityStartTimestamp,
            string reqValidityEndTimestamp
        )
        {
            if (Utils.IsZkSync(_chainId))
            {
                throw new InvalidOperationException("Account Permissions are not supported in ZkSync");
            }

            var request = new SignerPermissionRequest()
            {
                Signer = signerAddress,
                IsAdmin = 0,
                ApprovedTargets = approvedTargets,
                NativeTokenLimitPerTransaction = BigInteger.Parse(nativeTokenLimitPerTransactionInWei),
                PermissionStartTimestamp = BigInteger.Parse(permissionStartTimestamp),
                PermissionEndTimestamp = BigInteger.Parse(permissionEndTimestamp),
                ReqValidityStartTimestamp = BigInteger.Parse(reqValidityStartTimestamp),
                ReqValidityEndTimestamp = BigInteger.Parse(reqValidityEndTimestamp),
                Uid = Guid.NewGuid().ToByteArray()
            };

            var signature = await EIP712.GenerateSignature_SmartAccount("Account", "1", _chainId, await GetAddress(), request, _personalAccount);
            var data = new Contract(null, _accountContract.Abi, _accountContract.Address).GetFunction("setPermissionsForSigner").GetData(request, signature.HexToBytes());
            var txInput = new ThirdwebTransactionInput()
            {
                From = await GetAddress(),
                To = _accountContract.Address,
                Value = new HexBigInteger(0),
                Data = data
            };
            var txHash = await SendTransaction(txInput);
            return await ThirdwebTransaction.WaitForTransactionReceipt(Client, _chainId, txHash);
        }

        public async Task<ThirdwebTransactionReceipt> AddAdmin(string admin)
        {
            if (Utils.IsZkSync(_chainId))
            {
                throw new InvalidOperationException("Account Permissions are not supported in ZkSync");
            }

            var request = new SignerPermissionRequest()
            {
                Signer = admin,
                IsAdmin = 1,
                ApprovedTargets = new List<string>(),
                NativeTokenLimitPerTransaction = 0,
                PermissionStartTimestamp = Utils.GetUnixTimeStampNow() - 3600,
                PermissionEndTimestamp = Utils.GetUnixTimeStampIn10Years(),
                ReqValidityStartTimestamp = Utils.GetUnixTimeStampNow() - 3600,
                ReqValidityEndTimestamp = Utils.GetUnixTimeStampIn10Years(),
                Uid = Guid.NewGuid().ToByteArray()
            };

            var signature = await EIP712.GenerateSignature_SmartAccount("Account", "1", _chainId, await GetAddress(), request, _personalAccount);
            var data = new Contract(null, _accountContract.Abi, _accountContract.Address).GetFunction("setPermissionsForSigner").GetData(request, signature.HexToBytes());
            var txInput = new ThirdwebTransactionInput()
            {
                From = await GetAddress(),
                To = _accountContract.Address,
                Value = new HexBigInteger(0),
                Data = data
            };
            var txHash = await SendTransaction(txInput);
            return await ThirdwebTransaction.WaitForTransactionReceipt(Client, _chainId, txHash);
        }

        public async Task<ThirdwebTransactionReceipt> RemoveAdmin(string admin)
        {
            if (Utils.IsZkSync(_chainId))
            {
                throw new InvalidOperationException("Account Permissions are not supported in ZkSync");
            }

            var request = new SignerPermissionRequest()
            {
                Signer = admin,
                IsAdmin = 2,
                ApprovedTargets = new List<string>(),
                NativeTokenLimitPerTransaction = 0,
                PermissionStartTimestamp = Utils.GetUnixTimeStampNow() - 3600,
                PermissionEndTimestamp = Utils.GetUnixTimeStampIn10Years(),
                ReqValidityStartTimestamp = Utils.GetUnixTimeStampNow() - 3600,
                ReqValidityEndTimestamp = Utils.GetUnixTimeStampIn10Years(),
                Uid = Guid.NewGuid().ToByteArray()
            };

            var signature = await EIP712.GenerateSignature_SmartAccount("Account", "1", _chainId, await GetAddress(), request, _personalAccount);
            var data = new Contract(null, _accountContract.Abi, _accountContract.Address).GetFunction("setPermissionsForSigner").GetData(request, signature.HexToBytes());
            var txInput = new ThirdwebTransactionInput()
            {
                From = await GetAddress(),
                To = _accountContract.Address,
                Value = new HexBigInteger(0),
                Data = data
            };
            var txHash = await SendTransaction(txInput);
            return await ThirdwebTransaction.WaitForTransactionReceipt(Client, _chainId, txHash);
        }

        public Task<string> SignTypedDataV4(string json)
        {
            return _personalAccount.SignTypedDataV4(json);
        }

        public Task<string> SignTypedDataV4<T, TDomain>(T data, TypedData<TDomain> typedData)
            where TDomain : IDomain
        {
            return _personalAccount.SignTypedDataV4(data, typedData);
        }

        public Task<string> RecoverAddressFromTypedDataV4<T, TDomain>(T data, TypedData<TDomain> typedData, string signature)
            where TDomain : IDomain
        {
            return _personalAccount.RecoverAddressFromTypedDataV4(data, typedData, signature);
        }

        public async Task<BigInteger> EstimateUserOperationGas(ThirdwebTransactionInput transaction, BigInteger chainId)
        {
            if (Utils.IsZkSync(_chainId))
            {
                throw new Exception("User Operations are not supported in ZkSync");
            }

            var signedOp = await SignUserOp(transaction, null, simulation: true);
            if (signedOp is UserOperationV6)
            {
                var castSignedOp = signedOp as UserOperationV6;
                var cost = castSignedOp.CallGasLimit + castSignedOp.VerificationGasLimit + castSignedOp.PreVerificationGas;
                return cost;
            }
            else if (signedOp is UserOperationV7)
            {
                var castSignedOp = signedOp as UserOperationV7;
                var cost =
                    castSignedOp.CallGasLimit + castSignedOp.VerificationGasLimit + castSignedOp.PreVerificationGas + castSignedOp.PaymasterVerificationGasLimit + castSignedOp.PaymasterPostOpGasLimit;
                return cost;
            }
            else
            {
                throw new Exception("Invalid signed operation type");
            }
        }

        public async Task<string> SignTransaction(ThirdwebTransactionInput transaction)
        {
            if (Utils.IsZkSync(_chainId))
            {
                throw new Exception("Offline Signing is not supported in ZkSync");
            }

            var signedOp = await SignUserOp(transaction);
            if (signedOp is UserOperationV6)
            {
                var encodedOp = EncodeUserOperation(signedOp as UserOperationV6);
                return JsonConvert.SerializeObject(encodedOp);
            }
            else if (signedOp is UserOperationV7)
            {
                var encodedOp = EncodeUserOperation(signedOp as UserOperationV7);
                return JsonConvert.SerializeObject(encodedOp);
            }
            else
            {
                throw new Exception("Invalid signed operation type");
            }
        }

        public async Task<bool> IsConnected()
        {
            return Utils.IsZkSync(_chainId) ? await _personalAccount.IsConnected() : _accountContract != null;
        }

        public Task Disconnect()
        {
            _accountContract = null;
            return Task.CompletedTask;
        }

        public async Task<string> Authenticate(
            string domain,
            BigInteger chainId,
            string authPayloadPath = "/auth/payload",
            string authLoginPath = "/auth/login",
            IThirdwebHttpClient httpClientOverride = null
        )
        {
            var payloadURL = domain + authPayloadPath;
            var loginURL = domain + authLoginPath;

            var payloadBodyRaw = new { address = await GetAddress(), chainId = chainId.ToString() };
            var payloadBody = JsonConvert.SerializeObject(payloadBodyRaw);

            var httpClient = httpClientOverride ?? Client.HttpClient;

            var payloadContent = new StringContent(payloadBody, Encoding.UTF8, "application/json");
            var payloadResponse = await httpClient.PostAsync(payloadURL, payloadContent);
            _ = payloadResponse.EnsureSuccessStatusCode();
            var payloadString = await payloadResponse.Content.ReadAsStringAsync();

            var loginBodyRaw = JsonConvert.DeserializeObject<LoginPayload>(payloadString);
            var payloadToSign = Utils.GenerateSIWE(loginBodyRaw.payload);

            loginBodyRaw.signature = await PersonalSign(payloadToSign);
            var loginBody = JsonConvert.SerializeObject(new { payload = loginBodyRaw });

            var loginContent = new StringContent(loginBody, Encoding.UTF8, "application/json");
            var loginResponse = await httpClient.PostAsync(loginURL, loginContent);
            _ = loginResponse.EnsureSuccessStatusCode();
            var responseString = await loginResponse.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}
