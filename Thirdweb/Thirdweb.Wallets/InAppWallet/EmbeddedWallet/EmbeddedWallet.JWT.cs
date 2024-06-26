using System.Threading.Tasks;

namespace Thirdweb.EWS
{
    internal partial class EmbeddedWallet
    {
        public async Task<VerifyResult> SignInWithJwtAsync(string jwt, string encryptionKey, string recoveryCode)
        {
            Server.VerifyResult result = await server.VerifyJwtAsync(jwt).ConfigureAwait(false);
            return await PostAuthSetup(result, recoveryCode, encryptionKey, "JWT").ConfigureAwait(false);
        }
    }
}
