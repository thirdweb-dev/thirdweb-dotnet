﻿[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Thirdweb.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Thirdweb.Console")]

namespace Thirdweb
{
    public class ThirdwebClient
    {
        internal string SecretKey { get; }
        internal string ClientId { get; }
        internal string BundleId { get; }
        internal ITimeoutOptions FetchTimeoutOptions { get; }

        public ThirdwebClient(string clientId = null, string secretKey = null, string bundleId = null, ITimeoutOptions fetchTimeoutOptions = null)
        {
            if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("ClientId or SecretKey must be provided");
            }

            if (!string.IsNullOrEmpty(secretKey))
            {
                ClientId = Utils.ComputeClientIdFromSecretKey(secretKey);
                SecretKey = secretKey;
            }
            else
            {
                ClientId = clientId;
            }

            BundleId = bundleId;

            FetchTimeoutOptions = fetchTimeoutOptions ?? new TimeoutOptions();
        }
    }
}
