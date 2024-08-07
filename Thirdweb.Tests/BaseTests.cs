﻿using dotenv.net;

namespace Thirdweb.Tests;

public class BaseTests
{
    protected readonly ITestOutputHelper _output;
    protected readonly string? _secretKey;
    protected readonly string? _clientIdBundleIdOnly;
    protected readonly string? _bundleIdBundleIdOnly;

    public BaseTests(ITestOutputHelper output)
    {
        DotEnv.Load();
        _output = output;
        _secretKey = Environment.GetEnvironmentVariable("THIRDWEB_SECRET_KEY");
        _clientIdBundleIdOnly = Environment.GetEnvironmentVariable("THIRDWEB_CLIENT_ID_BUNDLE_ID_ONLY");
        _bundleIdBundleIdOnly = Environment.GetEnvironmentVariable("THIRDWEB_BUNDLE_ID_BUNDLE_ID_ONLY");

        _output.WriteLine($"Started {GetType().FullName}");
    }

    [Fact(Timeout = 120000)]
    public void DotEnvTest()
    {
        Assert.NotNull(_secretKey);
    }
}
