﻿using System.Numerics;
using System.Reflection;

namespace Thirdweb.Tests;

public class RpcTests : BaseTests
{
    public RpcTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public async Task GetBlockNumber()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey, fetchTimeoutOptions: new TimeoutOptions(rpc: 10000));
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var blockNumber = await rpc.SendRequestAsync<string>("eth_blockNumber");
        Assert.NotNull(blockNumber);
        Assert.StartsWith("0x", blockNumber);
    }

    [Fact]
    public async Task TestAuth()
    {
        var client = ThirdwebClient.Create(clientId: "hi", fetchTimeoutOptions: new TimeoutOptions(rpc: 60000));
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        _ = await Assert.ThrowsAsync<HttpRequestException>(async () => await rpc.SendRequestAsync<string>("eth_blockNumber"));
    }

    [Fact]
    public async Task TestTimeout()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey, fetchTimeoutOptions: new TimeoutOptions(rpc: 0));
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        _ = await Assert.ThrowsAsync<TimeoutException>(async () => await rpc.SendRequestAsync<string>("eth_chainId"));
    }

    [Fact]
    public async Task TestBatch()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var req = rpc.SendRequestAsync<string>("eth_blockNumber");
        _ = await rpc.SendRequestAsync<string>("eth_chainId");
        var blockNumberTasks = new List<Task<string>>();
        for (var i = 0; i < 100; i++)
        {
            blockNumberTasks.Add(rpc.SendRequestAsync<string>("eth_blockNumber"));
        }
        var results = await Task.WhenAll(blockNumberTasks);
        Assert.Equal(100, results.Length);
        Assert.All(results, result => Assert.StartsWith("0x", result));
        Assert.All(results, result => Assert.Equal(results[0], result));
    }

    [Fact]
    public async Task TestDeserialization()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await rpc.SendRequestAsync<BigInteger>("eth_blockNumber"));
        Assert.Equal("Failed to deserialize RPC response.", exception.Message);
    }

    [Fact]
    public void TestBadInitialization()
    {
        var clientException = Assert.Throws<ArgumentNullException>(() => ThirdwebRPC.GetRpcInstance(null, 0));
        Assert.Equal("client", clientException.ParamName);
        var chainIdException = Assert.Throws<ArgumentException>(() => ThirdwebRPC.GetRpcInstance(ThirdwebClient.Create(secretKey: _secretKey), 0));
        Assert.Equal("Invalid Chain ID", chainIdException.Message);
    }

    [Fact]
    public async Task TestBundleIdRpc()
    {
        var client = ThirdwebClient.Create(clientId: _clientIdBundleIdOnly, bundleId: _bundleIdBundleIdOnly);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var blockNumber = await rpc.SendRequestAsync<string>("eth_blockNumber");
        Assert.NotNull(blockNumber);
        Assert.StartsWith("0x", blockNumber);
    }

    [Fact]
    public async Task TestRpcError()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var exception = await Assert.ThrowsAsync<Exception>(async () => await rpc.SendRequestAsync<string>("eth_invalidMethod"));
        Assert.Contains("RPC Error for request", exception.Message);
    }

    [Fact]
    public async Task TestDispose()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        rpc.Dispose();
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => rpc.SendRequestAsync<string>("eth_blockNumber"));
    }

    [Fact]
    public async Task TestCache()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var blockNumber1 = await rpc.SendRequestAsync<string>("eth_blockNumber");
        await Task.Delay(100);
        var blockNumber2 = await rpc.SendRequestAsync<string>("eth_blockNumber");
        Assert.Equal(blockNumber1, blockNumber2);
    }

    [Fact]
    public async Task TestBatchSizeLimit()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        var rpc = ThirdwebRPC.GetRpcInstance(client, 1);
        var blockNumberTasks = new List<Task<string>>();
        for (var i = 0; i < 101; i++)
        {
            blockNumberTasks.Add(rpc.SendRequestAsync<string>("eth_blockNumber"));
        }
        var results = await Task.WhenAll(blockNumberTasks);
        Assert.Equal(101, results.Length);
        Assert.All(results, result => Assert.StartsWith("0x", result));
    }

    [Fact]
    public void Timer_StartsAndStops()
    {
        var timer = new ThirdwebRPCTimer(TimeSpan.FromMilliseconds(100));
        timer.Start();
        Assert.True(IsTimerRunning(timer));

        timer.Stop();
        Assert.False(IsTimerRunning(timer));
    }

    [Fact]
    public async Task Timer_ElapsedEventFires()
    {
        var timer = new ThirdwebRPCTimer(TimeSpan.FromMilliseconds(100));
        var eventFired = false;

        timer.Elapsed += () => eventFired = true;
        timer.Start();

        await Task.Delay(200); // Wait for the timer to elapse at least once
        Assert.True(eventFired);

        timer.Stop();
    }

    [Fact]
    public void Timer_DisposeStopsTimer()
    {
        var timer = new ThirdwebRPCTimer(TimeSpan.FromMilliseconds(100));
        timer.Start();
        timer.Dispose();
        Assert.False(IsTimerRunning(timer));
    }

    private bool IsTimerRunning(ThirdwebRPCTimer timer)
    {
        var fieldInfo = typeof(ThirdwebRPCTimer).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null)
        {
            throw new InvalidOperationException("The field '_isRunning' was not found.");
        }

        var value = fieldInfo.GetValue(timer);
        if (value == null)
        {
            throw new InvalidOperationException("The field '_isRunning' value is null.");
        }

        return (bool)value;
    }
}
