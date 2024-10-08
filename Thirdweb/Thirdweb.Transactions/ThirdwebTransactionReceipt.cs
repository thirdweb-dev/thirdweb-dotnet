﻿using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thirdweb;

/// <summary>
/// Represents the receipt of a transaction.
/// </summary>
public class ThirdwebTransactionReceipt
{
    /// <summary>
    /// Gets or sets the transaction hash.
    /// </summary>
    [JsonProperty(PropertyName = "transactionHash")]
    public string TransactionHash { get; set; }

    /// <summary>
    /// Gets or sets the transaction index within the block.
    /// </summary>
    [JsonProperty(PropertyName = "transactionIndex")]
    public HexBigInteger TransactionIndex { get; set; }

    /// <summary>
    /// Gets or sets the hash of the block containing the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "blockHash")]
    public string BlockHash { get; set; }

    /// <summary>
    /// Gets or sets the number of the block containing the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "blockNumber")]
    public HexBigInteger BlockNumber { get; set; }

    /// <summary>
    /// Gets or sets the address of the sender.
    /// </summary>
    [JsonProperty(PropertyName = "from")]
    public string From { get; set; }

    /// <summary>
    /// Gets or sets the address of the recipient.
    /// </summary>
    [JsonProperty(PropertyName = "to")]
    public string To { get; set; }

    /// <summary>
    /// Gets or sets the cumulative gas used by the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "cumulativeGasUsed")]
    public HexBigInteger CumulativeGasUsed { get; set; }

    /// <summary>
    /// Gets or sets the gas used by the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "gasUsed")]
    public HexBigInteger GasUsed { get; set; }

    /// <summary>
    /// Gets or sets the effective gas price for the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "effectiveGasPrice")]
    public HexBigInteger EffectiveGasPrice { get; set; }

    /// <summary>
    /// Gets or sets the contract address created by the transaction, if applicable.
    /// </summary>
    [JsonProperty(PropertyName = "contractAddress")]
    public string ContractAddress { get; set; }

    /// <summary>
    /// Gets or sets the status of the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "status")]
    public HexBigInteger Status { get; set; }

    /// <summary>
    /// Gets or sets the logs generated by the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "logs")]
    public JArray Logs { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    [JsonProperty(PropertyName = "type")]
    public HexBigInteger Type { get; set; }

    /// <summary>
    /// Gets or sets the logs bloom filter.
    /// </summary>
    [JsonProperty(PropertyName = "logsBloom")]
    public string LogsBloom { get; set; }

    /// <summary>
    /// Gets or sets the root of the transaction.
    /// </summary>
    [JsonProperty(PropertyName = "root")]
    public string Root { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
