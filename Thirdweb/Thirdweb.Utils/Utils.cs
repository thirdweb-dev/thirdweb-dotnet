using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;

namespace Thirdweb
{
    /// <summary>
    /// Provides utility methods for various operations.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Computes the client ID from the given secret key.
        /// </summary>
        /// <param name="secretKey">The secret key.</param>
        /// <returns>The computed client ID.</returns>
        public static string ComputeClientIdFromSecretKey(string secretKey)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));
            return BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0, 32);
        }

        /// <summary>
        /// Concatenates the given hex strings.
        /// </summary>
        /// <param name="hexStrings">The hex strings to concatenate.</param>
        /// <returns>The concatenated hex string.</returns>
        public static string HexConcat(params string[] hexStrings)
        {
            var hex = new StringBuilder("0x");

            foreach (var hexStr in hexStrings)
            {
                _ = hex.Append(hexStr[2..]);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Hashes the given message bytes with a prefixed message.
        /// </summary>
        /// <param name="messageBytes">The message bytes to hash.</param>
        /// <returns>The hashed message bytes.</returns>
        public static byte[] HashPrefixedMessage(this byte[] messageBytes)
        {
            var signer = new EthereumMessageSigner();
            return signer.HashPrefixedMessage(messageBytes);
        }

        /// <summary>
        /// Hashes the given message with a prefixed message.
        /// </summary>
        /// <param name="message">The message to hash.</param>
        /// <returns>The hashed message.</returns>
        public static string HashPrefixedMessage(this string message)
        {
            var signer = new EthereumMessageSigner();
            return signer.HashPrefixedMessage(Encoding.UTF8.GetBytes(message)).ToHex(true);
        }

        /// <summary>
        /// Hashes the given message bytes.
        /// </summary>
        /// <param name="messageBytes">The message bytes to hash.</param>
        /// <returns>The hashed message bytes.</returns>
        public static byte[] HashMessage(this byte[] messageBytes)
        {
            return Sha3Keccack.Current.CalculateHash(messageBytes);
        }

        /// <summary>
        /// Hashes the given message.
        /// </summary>
        /// <param name="message">The message to hash.</param>
        /// <returns>The hashed message.</returns>
        public static string HashMessage(this string message)
        {
            return Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(message)).ToHex(true);
        }

        /// <summary>
        /// Converts the given bytes to a hex string.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The hex string.</returns>
        public static string BytesToHex(this byte[] bytes)
        {
            return bytes.ToHex(true);
        }

        /// <summary>
        /// Converts the given hex string to bytes.
        /// </summary>
        /// <param name="hex">The hex string to convert.</param>
        /// <returns>The bytes.</returns>
        public static byte[] HexToBytes(this string hex)
        {
            return hex.HexToByteArray();
        }

        /// <summary>
        /// Converts the given string to a hex string.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The hex string.</returns>
        public static string StringToHex(this string str)
        {
            return "0x" + Encoding.UTF8.GetBytes(str).ToHex();
        }

        /// <summary>
        /// Converts the given hex string to a regular string.
        /// </summary>
        /// <param name="hex">The hex string to convert.</param>
        /// <returns>The regular string.</returns>
        public static string HexToString(this string hex)
        {
            var array = HexToBytes(hex);
            return Encoding.UTF8.GetString(array, 0, array.Length);
        }

        /// <summary>
        /// Gets the current Unix timestamp.
        /// </summary>
        /// <returns>The current Unix timestamp.</returns>
        public static long GetUnixTimeStampNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Gets the Unix timestamp for 10 years from now.
        /// </summary>
        /// <returns>The Unix timestamp for 10 years from now.</returns>
        public static long GetUnixTimeStampIn10Years()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60 * 60 * 24 * 365 * 10;
        }

        /// <summary>
        /// Replaces the IPFS URI with a specified gateway.
        /// </summary>
        /// <param name="uri">The URI to replace.</param>
        /// <param name="gateway">The gateway to use.</param>
        /// <returns>The replaced URI.</returns>
        public static string ReplaceIPFS(this string uri, string gateway = null)
        {
            gateway ??= Constants.FALLBACK_IPFS_GATEWAY;
            return !string.IsNullOrEmpty(uri) && uri.StartsWith("ipfs://") ? uri.Replace("ipfs://", gateway) : uri;
        }

        /// <summary>
        /// Converts the given ether value to wei.
        /// </summary>
        /// <param name="eth">The ether value to convert.</param>
        /// <returns>The wei value.</returns>
        public static string ToWei(this string eth)
        {
            if (!double.TryParse(eth, NumberStyles.Number, CultureInfo.InvariantCulture, out var ethDouble))
            {
                throw new ArgumentException("Invalid eth value.");
            }

            var wei = (BigInteger)(ethDouble * Constants.DECIMALS_18);
            return wei.ToString();
        }

        /// <summary>
        /// Converts the given wei value to ether.
        /// </summary>
        /// <param name="wei">The wei value to convert.</param>
        /// <param name="decimalsToDisplay">The number of decimals to display.</param>
        /// <param name="addCommas">Whether to add commas to the output.</param>
        /// <returns>The ether value.</returns>
        public static string ToEth(this string wei, int decimalsToDisplay = 4, bool addCommas = false)
        {
            return FormatERC20(wei, decimalsToDisplay, 18, addCommas);
        }

        /// <summary>
        /// Formats the given ERC20 token value.
        /// </summary>
        /// <param name="wei">The wei value to format.</param>
        /// <param name="decimalsToDisplay">The number of decimals to display.</param>
        /// <param name="decimals">The number of decimals of the token.</param>
        /// <param name="addCommas">Whether to add commas to the output.</param>
        /// <returns>The formatted token value.</returns>
        public static string FormatERC20(this string wei, int decimalsToDisplay = 4, int decimals = 18, bool addCommas = false)
        {
            if (!BigInteger.TryParse(wei, out var weiBigInt))
            {
                throw new ArgumentException("Invalid wei value.");
            }

            var eth = (double)weiBigInt / Math.Pow(10.0, decimals);
            var format = addCommas ? "#,0" : "#0";
            if (decimalsToDisplay > 0)
            {
                format += ".";
                format += new string('0', decimalsToDisplay);
            }

            return eth.ToString(format);
        }

        /// <summary>
        /// Generates a Sign-In With Ethereum (SIWE) message.
        /// </summary>
        /// <param name="loginPayloadData">The login payload data.</param>
        /// <returns>The generated SIWE message.</returns>
        public static string GenerateSIWE(LoginPayloadData loginPayloadData)
        {
            if (loginPayloadData == null)
            {
                throw new ArgumentNullException(nameof(loginPayloadData));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.Domain))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.Domain));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.Address))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.Address));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.Version))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.Version));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.ChainId))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.ChainId));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.Nonce))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.Nonce));
            }
            else if (string.IsNullOrEmpty(loginPayloadData.IssuedAt))
            {
                throw new ArgumentNullException(nameof(loginPayloadData.IssuedAt));
            }

            var resourcesString = loginPayloadData.Resources != null ? "\nResources:" + string.Join("", loginPayloadData.Resources.Select(r => $"\n- {r}")) : string.Empty;
            var payloadToSign =
                $"{loginPayloadData.Domain} wants you to sign in with your Ethereum account:"
                + $"\n{loginPayloadData.Address}\n\n"
                + $"{(string.IsNullOrEmpty(loginPayloadData.Statement) ? "" : $"{loginPayloadData.Statement}\n")}"
                + $"{(string.IsNullOrEmpty(loginPayloadData.Uri) ? "" : $"\nURI: {loginPayloadData.Uri}")}"
                + $"\nVersion: {loginPayloadData.Version}"
                + $"\nChain ID: {loginPayloadData.ChainId}"
                + $"\nNonce: {loginPayloadData.Nonce}"
                + $"\nIssued At: {loginPayloadData.IssuedAt}"
                + $"{(string.IsNullOrEmpty(loginPayloadData.ExpirationTime) ? "" : $"\nExpiration Time: {loginPayloadData.ExpirationTime}")}"
                + $"{(string.IsNullOrEmpty(loginPayloadData.InvalidBefore) ? "" : $"\nNot Before: {loginPayloadData.InvalidBefore}")}"
                + resourcesString;
            return payloadToSign;
        }

        /// <summary>
        /// Checks if the chain ID corresponds to zkSync.
        /// </summary>
        /// <param name="chainId">The chain ID.</param>
        /// <returns>True if it is a zkSync chain ID, otherwise false.</returns>
        public static bool IsZkSync(BigInteger chainId)
        {
            return chainId.Equals(324) || chainId.Equals(300) || chainId.Equals(302);
        }

        /// <summary>
        /// Converts an Ethereum address to its checksum format.
        /// </summary>
        /// <param name="address">The Ethereum address.</param>
        /// <returns>The checksummed Ethereum address.</returns>
        public static string ToChecksumAddress(this string address)
        {
            return new AddressUtil().ConvertToChecksumAddress(address);
        }

        /// <summary>
        /// Decodes all events of the specified type from the transaction receipt logs.
        /// </summary>
        /// <typeparam name="TEventDTO">The event DTO type.</typeparam>
        /// <param name="transactionReceipt">The transaction receipt.</param>
        /// <returns>A list of decoded events.</returns>
        public static List<EventLog<TEventDTO>> DecodeAllEvents<TEventDTO>(this ThirdwebTransactionReceipt transactionReceipt)
            where TEventDTO : new()
        {
            return transactionReceipt.Logs.DecodeAllEvents<TEventDTO>();
        }

        /// <summary>
        /// Adjusts the value's decimals.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="fromDecimals">The original number of decimals.</param>
        /// <param name="toDecimals">The target number of decimals.</param>
        /// <returns>The value adjusted to the new decimals.</returns>
        public static BigInteger AdjustDecimals(this BigInteger value, int fromDecimals, int toDecimals)
        {
            var differenceInDecimals = fromDecimals - toDecimals;

            if (differenceInDecimals > 0)
            {
                return value / BigInteger.Pow(10, differenceInDecimals);
            }
            else if (differenceInDecimals < 0)
            {
                return value * BigInteger.Pow(10, -differenceInDecimals);
            }

            return value;
        }

        public static async Task<ThirdwebChainData> FetchThirdwebChainDataAsync(ThirdwebClient client, BigInteger chainId)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (chainId <= 0)
            {
                throw new ArgumentException("Invalid chain ID.");
            }

            var url = $"https://api.thirdweb-dev.com/v1/chains/{chainId}";
            try
            {
                var response = await client.HttpClient.GetAsync(url).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ThirdwebChainDataResponse>(json);

                return deserializedResponse == null || deserializedResponse.Error != null
                    ? throw new Exception($"Failed to fetch chain data for chain ID {chainId}. Error: {Newtonsoft.Json.JsonConvert.SerializeObject(deserializedResponse?.Error)}")
                    : deserializedResponse.Data;
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP request error while fetching chain data for chain ID {chainId}: {httpEx.Message}", httpEx);
            }
            catch (Newtonsoft.Json.JsonException jsonEx)
            {
                throw new Exception($"JSON deserialization error while fetching chain data for chain ID {chainId}: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while fetching chain data for chain ID {chainId}: {ex.Message}", ex);
            }
        }

        public static int GetEntryPointVersion(string address)
        {
            address = address.ToChecksumAddress();
            return address switch
            {
                Constants.ENTRYPOINT_ADDRESS_V06 => 6,
                Constants.ENTRYPOINT_ADDRESS_V07 => 7,
                _ => 6,
            };
        }

        public static byte[] HexToBytes32(this string hex)
        {
            if (hex.StartsWith("0x"))
            {
                hex = hex.Substring(2);
            }

            if (hex.Length > 64)
            {
                throw new ArgumentException("Hex string is too long to fit into 32 bytes.");
            }

            hex = hex.PadLeft(64, '0');

            var bytes = new byte[32];
            for (var i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(hex.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return bytes;
        }
    }
}
