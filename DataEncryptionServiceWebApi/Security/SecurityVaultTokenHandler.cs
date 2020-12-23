using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.Integration.Vault;
using DataEncryptionService.Telemetry;
using DataEncryptionService.WebApi.Telemetry;
using DataEncryptionService.WebApi.Telemetry.Names;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token.Models;
using VaultSharp.V1.Commons;

namespace DataEncryptionService.WebApi.Security
{
    public class SecurityVaultTokenHandler : ISecurityVaultTokenHandler
    {
        private class CachedTokenInfo
        {
            public CachedTokenInfo(TokenInfo tokenInfo)
            {
                EntityId = tokenInfo.EntityId;
                DisplayName = tokenInfo.DisplayName;

                string userName = null;
                tokenInfo.Metadata?.TryGetValue("username", out userName);
                UserName = string.IsNullOrEmpty(userName) ? DisplayName : userName;

                // Policies on a Vault token are either assigned directly or inherited (i.e. identify policies obtained by membership to one or more role hierarchies)
                Policies = new HashSet<string>(tokenInfo.Policies);
                Policies.UnionWith(tokenInfo.IdentityPolicies);
            }

            public string EntityId { get; set; }
            public string DisplayName { get; set; }
            public string UserName { get; set; }
            public HashSet<string> Policies { get; set; }

            public bool AssignedPoliciesMatch(HashSet<string> allRequiredAccessPolicies)
            {
                // Check the token has ALL the required Access Policies assigned to it
                return Policies.Intersect(allRequiredAccessPolicies).Count() == allRequiredAccessPolicies.Count;
            }
        }

        private readonly ILogger _log;
        private readonly ITelemetrySourceClient _telemetry;
        private readonly IVaultClient _vaultClient;
        private readonly IMemoryCache _cache;
        private readonly int _cacheItemTTL;

        public SecurityVaultTokenHandler(
                    ServiceConfigPolicies policyConfiguration,
                    ILogger<SecurityVaultTokenHandler> log,
                    ITelemetrySourceClient telemetry,
                    IMemoryCache cache,
                    VaultServiceConfiguration vaultConfiguration,
                    IVaultClientFactory factory)
        {
            _log = log;
            _telemetry = telemetry;
            _cache = cache;
            _cacheItemTTL = policyConfiguration.API.TokenCacheTTL; // In seconds. 
            if (_cacheItemTTL < 1 || _cacheItemTTL > 900) // 900 seconds == 15 minutes
            {
                _cacheItemTTL = 300; // Default to 5 minutes
            }

            _log.LogInformation($"Security Configuration: Vault Service {vaultConfiguration.ServiceUrl}");

            (IVaultClient client, string errorMessage) = factory.CreateClient(vaultConfiguration);
            if (null == client)
            {
                string message = $"Cannot create Vault client for Security Token handler. {errorMessage}";
                _log.LogError(message);
                throw new ApplicationException(message);
            }
            else
            {
                _vaultClient = client;
            }
        }

        public async Task<TokenValidationResult> ValidateAccessTokenAsync(string accessToken, HashSet<string> allRequiredAccessPolicies, bool forceCacheRefresh)
        {
            string requestId = Guid.NewGuid().ToString();
            bool badToken = false, tokenCacheHit = false;
            var result = new TokenValidationResult() { IsAllowed = false };

            var spans = new List<TelemetrySpan>();
            try
            {
                var parentSpan = SpanMeasure.Start(SpanName.Token_Validation_Request, spans);
                try
                {
                    CachedTokenInfo tokenInfo = _cache?.Get<CachedTokenInfo>(accessToken);
                    if (tokenInfo is null)
                    {
                        Secret<TokenInfo> lookupResult = null;
                        using (SpanMeasure.Start(SpanName.Token_Validation_Request, spans, parentSpan))
                        {
                            lookupResult = await _vaultClient.V1.Auth.Token.LookupAsync(accessToken);
                        }
                        tokenInfo = new CachedTokenInfo(lookupResult.Data);
                        _cache?.Set(accessToken, tokenInfo, TimeSpan.FromSeconds(_cacheItemTTL));
                    }
                    else
                    {
                        tokenCacheHit = true;
                    }

                    // Save some token properties
                    result.EntityId = tokenInfo.EntityId;
                    result.DisplayName = tokenInfo.DisplayName;
                    result.UserName = tokenInfo.UserName;

                    // Check the token has ALL the required Access Policies assigned to it
                    if (tokenInfo.AssignedPoliciesMatch(allRequiredAccessPolicies))
                    {
                        result.IsAllowed = true;
                    }
                    else
                    {
                        result.Message = "Access Denied.";
                    }
                }
                catch (VaultApiException e)
                {
                    string errorMessage = e.ApiErrors.FirstOrDefault();
                    if (!string.IsNullOrEmpty(errorMessage) && errorMessage.Contains("bad token", StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Message = "Invalid Token.";
                        badToken = true;
                    }
                    else
                    {
                        _log.LogError(e, "Internal Error (VaultApiException): " + e.Message, requestId);
                        result.Message = "An internal error occurred accessing the authorization service. Access and Authorization cannot be verified at this time.";
                    }
                }
                catch (Exception e)
                {
                    _log.LogError(e, "Internal Error: " + e.Message, requestId);
                    result.Message = "An internal error occurred. Access and Authorization cannot be verified at this time.";
                }
            }
            finally
            {
                var eventAttributes = new Dictionary<string, object>
                {
                    { EventAttributes.TokenCacheHit, tokenCacheHit },
                    { EventAttributes.TokenAllowed, result.IsAllowed },
                    { EventAttributes.EntityId, result.EntityId },
                    { EventAttributes.DisplayName, result.DisplayName },
                    { EventAttributes.UserName, result.UserName }
                };
                // Never save the token itself in telemetry or logs, that is the actual access secret

                if (badToken)
                {
                    await _telemetry.RaiseErrorAsync((int)ErrorCode.Generic_Undefined_Error, result.Message, correlationKey: requestId);
                }

                await _telemetry.RaiseEventAsync(EventName.TokenValidationCompleted, spans, requestId, eventAttributes);
            }

            return result;
        }

        private static class EventAttributes
        {
            public const string TokenCacheHit = "TokenCacheHit";
            public const string TokenAllowed = "TokenAllowed";
            public const string EntityId = "EntityId";
            public const string DisplayName = "DisplayName";
            public const string UserName = "UserName";
        }
    }
}