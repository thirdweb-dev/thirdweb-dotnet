﻿namespace Thirdweb.EWS;

internal partial class EmbeddedWallet
{
    public async Task<Server.VerifyResult> SignInWithAuthEndpointAsync(string payload)
    {
        return await this._server.VerifyAuthEndpointAsync(payload).ConfigureAwait(false);
    }
}
