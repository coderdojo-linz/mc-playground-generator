namespace GeneratorFunction;

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

public class ArmTokenCache
{
    private readonly IConfiguration configuration;
    private string? token;
    private DateTime? expires;
    private readonly object tokenLockObject = new();

    public ArmTokenCache(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<string> Acquire()
    {
        if (token is null || expires is null || DateTime.UtcNow > expires.Value.AddSeconds(-15))
        {
            // Get token
            var cred = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                VisualStudioTenantId = configuration["TenantId"],
            });
            var newToken = await cred.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/" }));

            // Parse token to get expiration time
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(newToken.Token);
            var expUnixTime = int.Parse(jwt.Claims.First(c => c.Type == "exp").Value);

            // Update token cache
            lock (tokenLockObject)
            {
                token = newToken.Token;
                expires = DateTime.UnixEpoch.AddSeconds(expUnixTime);
            }
        }

        // return token
        return token;
    }
}
