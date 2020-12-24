using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataEncryptionService.WebApi.Security
{
    public interface ISecurityVaultTokenHandler
    {
        Task<TokenValidationResult> ValidateAccessTokenAsync(string accessToken, HashSet<string> allRequiredAccessPolicies, bool forceCacheRefresh = false);
    }
}