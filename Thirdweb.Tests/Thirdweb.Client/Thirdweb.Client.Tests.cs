﻿namespace Thirdweb.Tests.Client;

public class ClientTests : BaseTests
{
    public ClientTests(ITestOutputHelper output)
        : base(output) { }

    [Fact(Timeout = 120000)]
    public void NoSecretKeyNoClientId()
    {
        Assert.Throws<InvalidOperationException>(() => ThirdwebClient.Create());
    }

    [Fact(Timeout = 120000)]
    public void SecretKeyInitialization()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        Assert.NotNull(client.ClientId);
        Assert.NotNull(client.SecretKey);
        Assert.Null(client.BundleId);
        Assert.Equal(client.ClientId, Utils.ComputeClientIdFromSecretKey(client.SecretKey));
        Assert.Equal(client.SecretKey, _secretKey);
    }

    [Fact(Timeout = 120000)]
    public void ClientIdInitialization()
    {
        var clientId = "test-client-id";
        var client = ThirdwebClient.Create(clientId: clientId);
        Assert.NotNull(client.ClientId);
        Assert.Null(client.SecretKey);
        Assert.Null(client.BundleId);
        Assert.Equal(client.ClientId, clientId);
    }

    [Fact(Timeout = 120000)]
    public void BundleIdInitialization()
    {
        var bundleId = "test-bundle-id";
        var exception = Assert.Throws<InvalidOperationException>(() => ThirdwebClient.Create(bundleId: bundleId));
        Assert.Equal("ClientId or SecretKey must be provided", exception.Message);
    }

    [Fact(Timeout = 120000)]
    public void ClientIdAndSecretKeyInitialization()
    {
        var clientId = "test-client-id";
        var client = ThirdwebClient.Create(clientId: clientId, secretKey: _secretKey);
        Assert.NotNull(client.ClientId);
        Assert.NotNull(client.SecretKey);
        Assert.Null(client.BundleId);
        Assert.NotEqual(client.ClientId, clientId);
        Assert.Equal(client.ClientId, Utils.ComputeClientIdFromSecretKey(client.SecretKey));
        Assert.Equal(client.SecretKey, _secretKey);
    }

    [Fact(Timeout = 120000)]
    public void ClientIdAndBundleIdInitialization()
    {
        var clientId = "test-client-id";
        var bundleId = "test-bundle-id";
        var client = ThirdwebClient.Create(clientId: clientId, bundleId: bundleId);
        Assert.NotNull(client.ClientId);
        Assert.NotNull(client.BundleId);
        Assert.Null(client.SecretKey);
        Assert.Equal(client.ClientId, clientId);
        Assert.Equal(client.BundleId, bundleId);
    }

    [Fact(Timeout = 120000)]
    public void SecretKeyAndBundleIdInitialization()
    {
        var bundleId = "test-bundle-id";
        var client = ThirdwebClient.Create(secretKey: _secretKey, bundleId: bundleId);
        Assert.NotNull(client.SecretKey);
        Assert.NotNull(client.BundleId);
        Assert.NotNull(client.ClientId);
        Assert.Equal(client.SecretKey, _secretKey);
        Assert.Equal(client.BundleId, bundleId);
        Assert.Equal(client.ClientId, Utils.ComputeClientIdFromSecretKey(client.SecretKey));
    }

    [Fact(Timeout = 120000)]
    public void TimeoutOptions()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey, fetchTimeoutOptions: new TimeoutOptions(storage: 30000, rpc: 30000, other: 30000));
        Assert.NotNull(client.FetchTimeoutOptions);
        Assert.Equal(30000, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Storage));
        Assert.Equal(30000, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Rpc));
        Assert.Equal(30000, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Other));
        Assert.Equal(Constants.DEFAULT_FETCH_TIMEOUT, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Other + 1));
    }

    [Fact(Timeout = 120000)]
    public void NoTimeoutOptions()
    {
        var client = ThirdwebClient.Create(secretKey: _secretKey);
        Assert.NotNull(client.FetchTimeoutOptions);
        Assert.Equal(Constants.DEFAULT_FETCH_TIMEOUT, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Storage));
        Assert.Equal(Constants.DEFAULT_FETCH_TIMEOUT, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Rpc));
        Assert.Equal(Constants.DEFAULT_FETCH_TIMEOUT, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Other));
        Assert.Equal(Constants.DEFAULT_FETCH_TIMEOUT, client.FetchTimeoutOptions.GetTimeout(TimeoutType.Other + 1));
    }
}
