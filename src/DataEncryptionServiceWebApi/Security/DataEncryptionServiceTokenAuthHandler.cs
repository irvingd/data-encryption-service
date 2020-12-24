using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataEncryptionService.WebApi.Security
{
    public class DataEncryptionServiceTokenAuthHandler : AuthenticationHandler<DataEncryptionServiceTokenAuthOptions>
    {
        private readonly ISecurityVaultTokenHandler _tokenHandler;
        private readonly HashSet<string> _requiredTokenPolicies;

        public DataEncryptionServiceTokenAuthHandler(
                        ServiceConfigPolicies policyConfiguration, 
                        ILogger<DataEncryptionServiceTokenAuthHandler> log,
                        IOptionsMonitor<DataEncryptionServiceTokenAuthOptions> options,
                        ILoggerFactory logger,
                        UrlEncoder encoder,
                        ISystemClock clock,
                        ISecurityVaultTokenHandler tokenHandler)
            : base(options, logger, encoder, clock)
        {
            // There MUST be at least one ACL policy required in the Access Token to use the API
            if (policyConfiguration?.API?.RequiredTokenPolicies?.Count > 0)
            {
                _requiredTokenPolicies = new HashSet<string>(policyConfiguration.API.RequiredTokenPolicies);
                _requiredTokenPolicies.RemoveWhere(p => string.IsNullOrWhiteSpace(p));
            }

            if (_requiredTokenPolicies is null || _requiredTokenPolicies.Count == 0)
            {
                log.LogError("Missing required configureation. At least one required access policy must be specified for the API.");
                throw new ApplicationException("Service is missing required configuration: API Access Policies");
            }
            else
            {
                log.LogInformation($"Security Configuration: Required Token Policies [{string.Join(", ", _requiredTokenPolicies)}]");
            }

            _tokenHandler = tokenHandler;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
            {
                return AuthenticateResult.Fail($"Missing Required Header: {Options.TokenHeaderName}");
            }

            //Request.Headers.TryGetValue(Options.TokenHeaderName, out var values);
            //string token = values.FirstOrDefault();
            string token = Request.Headers[Options.TokenHeaderName];
            var result = await _tokenHandler.ValidateAccessTokenAsync(token, _requiredTokenPolicies);
            if (result.IsAllowed)
            {
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, result.EntityId),
                    new Claim(ClaimTypes.Name, result.UserName),
                    new Claim(ClaimTypes.GivenName, result.DisplayName),
                };

                var id = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(id);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.Fail(result.Message);
        }
    }
}