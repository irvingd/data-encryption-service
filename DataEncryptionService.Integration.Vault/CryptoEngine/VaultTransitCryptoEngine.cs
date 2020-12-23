using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DataEncryptionService.Configuration;
using DataEncryptionService.CryptoEngines;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Transit;

namespace DataEncryptionService.Integration.Vault.CryptoEngine
{
    public class VaultTransitCryptoEngine : ICryptographicEngine
    {
        private readonly ILogger _log;
        private readonly IVaultClient _vaultClient;
        private readonly string _defaultMountPoint;
        private readonly string _defaultKeyName;
        private readonly bool _isConfigured;

        public VaultTransitCryptoEngine(DataEncryptionServiceConfiguration serviceConfig, ILogger<VaultTransitCryptoEngine> log, IVaultClientFactory clientFactory)
        {
            _log = log;

            var sb = new StringBuilder($"Initializing '{DisplayName}' ({EngineId}). ");
            ServiceConfigEncryptionEngine config = serviceConfig.Encryption.EngineConfigurations
                                                                                    .Where(x => x.EngineId == EngineId)
                                                                                    .FirstOrDefault();
            if (null != config)
            {
                config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.DefaultMountPoint, out string value);
                _defaultMountPoint = value?.Trim();

                config.Parameters.TryGetValue(WellKnownConstants.Vault.Configuration.DefaultKeyName, out value);
                _defaultKeyName = value?.Trim();

                sb.Append($"Mount Point: {_defaultMountPoint}, Transit Point: {_defaultKeyName}. ");
            }

            // Create a vault client if the configuration is properly set
            (IVaultClient client, string errorMessage) = clientFactory.CreateClient(serviceConfig.VaultService);
            if (null == client)
            {
                _log.LogError($"Cannot create Vault client for cryptographic engine: {errorMessage}");
            }
            else
            {
                _vaultClient = client;
                _isConfigured = true;
            }

            sb.Append($"Configured: {_isConfigured}.");
            _log.LogInformation(sb.ToString());
        }

        public string DisplayName => "Hashicorp Vault Transit Secret Engine";
        public Guid EngineId => WellKnownConstants.Vault.CryptoEngineUUID;
        public bool IsConfigured => _isConfigured;

        public async Task<EncryptionResult> EncryptAsync(IReadOnlyDictionary<string, string> kvClearText, Dictionary<string, object> parameters = null)
        {
            var result = EncryptionResult.CreateDefault();
            bool hasEmptyKey = kvClearText.Keys.Any(item => string.IsNullOrEmpty(item));
            if (hasEmptyKey)
            {
                // TODO: lets check for context (the KEY in the kvClearText dictionary) - don't allow requests without a context
                result.Code = ErrorCode.Crypto_Encryption_Context_Not_Set;
                result.Message = "At least one name-value pair in the kvClearText parameter has a null or empty string for its name.";
                return result;
            }

            (string mountPoint, ErrorCode code, string errorMessage) = GetMountPointFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            string encryptionKeyName;
            (encryptionKeyName, code, errorMessage) = GetEncryptionKeyNameFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            // See if the user specified a key version to use, otherwise NULL means use latest version
            var encOptions = new VaultSharp.V1.SecretsEngines.Transit.EncryptRequestOptions();
            encOptions.KeyVersion = Parameters.GetValue(parameters, CommonParameterNames.KeyVersion, 0);
            encOptions.KeyVersion = encOptions.KeyVersion == 0 ? null : encOptions.KeyVersion;

            ////////////////////////////////////////////////////////////////////////////////////////////
            // TODO: Refactor this block this when Vault issue is fixed: https://github.com/hashicorp/vault/issues/10232
            if (encOptions.KeyVersion.HasValue && kvClearText.Count > 1)
            {
                result.Code = ErrorCode.Crypto_Undefined_Error;
                result.Message = "ISSUE: Hashicorp Vault has a bug that prevents encrypting multiple items to a specific version of the key.  Please update this engine when the issue is resolved.";
                return result;
            }

            bool requestIsNonBatched = false;
            if (encOptions.KeyVersion.HasValue && kvClearText.Count == 1)
            {
                requestIsNonBatched = true;
                encOptions.Base64EncodedContext = kvClearText.First().Key.ToBase64();
                encOptions.Base64EncodedPlainText = kvClearText.First().Value.ToBase64();
                encOptions.Nonce = string.Empty;
            }
            else
            {
                encOptions.BatchedEncryptionItems = new List<EncryptionItem>();
                foreach (KeyValuePair<string, string> item in kvClearText)
                {
                    var encItem = new EncryptionItem()
                    {
                        Base64EncodedContext = item.Key.ToBase64(),
                        Base64EncodedPlainText = item.Value.ToBase64(),
                        Nonce = string.Empty
                    };
                    encOptions.BatchedEncryptionItems.Add(encItem);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////////

            // Make the request to the Vault server to encrypt the data items
            try
            {
                Secret<EncryptionResponse> encResponse = await _vaultClient.V1.Secrets.Transit.EncryptAsync(encryptionKeyName, encOptions, mountPoint);
                // Retrieve the encrypted values
                var kvCipherText = new Dictionary<string, string>();

                if (requestIsNonBatched && kvClearText.Count > 0)
                {
                    kvCipherText.Add(kvClearText.First().Key, encResponse.Data.CipherText);
                }
                else
                {
                    int position = 0;
                    foreach (var batchItem in encResponse.Data.BatchedResults)
                    {
                        kvCipherText.Add(kvClearText.Keys.ElementAt(position), batchItem.CipherText);
                        position++;
                    }
                }

                // Record the parameters used in this encryption request
                Dictionary<string, object> usedParameters = new Dictionary<string, object>()
                {
                    { CommonParameterNames.KeyScope, mountPoint },
                    { CommonParameterNames.KeyName, encryptionKeyName },
                };

                // TODO: improve this. get the key version from Vault or from the incoming parameters... don't rely on the text returned, it may change in the future
                usedParameters.Add(CommonParameterNames.KeyVersion, GetKeyVersionFromCipherText(kvCipherText.First().Value));

                result.RequestId = encResponse.RequestId;
                result.Data = kvCipherText;
                result.Parameters = usedParameters;
            }
            catch (VaultApiException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                (result.Code, result.Message) = MapVaultExceptionToErrorCode(e);
            }
            catch (HttpRequestException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Network_Connection_Error;
                result.Message = e.Message;
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Crypto_Undefined_Error;
                result.Message = e.Message;
            }

            return result;
        }

        public async Task<DecryptionResult> DecryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null)
        {
            var result = DecryptionResult.CreateDefault();
            
            (string mountPoint, ErrorCode code, string errorMessage) = GetMountPointFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            string encryptionKeyName;
            (encryptionKeyName, code, errorMessage) = GetEncryptionKeyNameFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            var decOptions = new DecryptRequestOptions();
            decOptions.BatchedDecryptionItems = new List<DecryptionItem>();

            foreach (KeyValuePair<string, string> item in kvCipherText)
            {
                var decItem = new DecryptionItem()
                {
                    Base64EncodedContext = item.Key.ToBase64(),
                    CipherText = item.Value,
                    Nonce = string.Empty
                };
                decOptions.BatchedDecryptionItems.Add(decItem);
            }

            try
            {
                Secret<DecryptionResponse> decResponse = await _vaultClient.V1.Secrets.Transit.DecryptAsync(encryptionKeyName, decOptions, mountPoint);

                int position = 0;
                foreach (var batchItem in decResponse.Data.BatchedResults)
                {
                    byte[] clearTextBytes = Convert.FromBase64String(batchItem.Base64EncodedPlainText);
                    string clearText = Encoding.UTF8.GetString(clearTextBytes);

                    result.Data.Add(kvCipherText.Keys.ElementAt(position), clearText);
                    position++;
                }
            }
            catch (VaultApiException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                (result.Code, result.Message) = MapVaultExceptionToErrorCode(e);
            }
            catch (HttpRequestException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Network_Connection_Error;
                result.Message = e.Message;
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Crypto_Undefined_Error;
                result.Message = e.Message;
            }

            return result;
        }

        public async Task<RotateKeyResult> RotateEncryptionKeyAsync(string keyName, Dictionary<string, object> parameters = null)
        {
            var result = RotateKeyResult.CreateDefault();

            if (string.IsNullOrWhiteSpace(keyName))
            {
                result.Code = ErrorCode.Crypto_Encryption_Key_Not_Specified;
                result.Message = "The encryption key was not specified explicitly or through configuration.";
                return result;
            }

            (string mountPoint, ErrorCode code, string errorMessage) = GetMountPointFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            try
            {
                await _vaultClient.V1.Secrets.Transit.RotateKeyAsync(keyName, mountPoint);
            }
            catch (VaultApiException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                (result.Code, result.Message) = MapVaultExceptionToErrorCode(e);
            }
            catch (HttpRequestException e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Network_Connection_Error;
                result.Message = e.Message;
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message, result.RequestId);
                result.Code = ErrorCode.Crypto_Undefined_Error;
                result.Message = e.Message;
            }

            return result;
        }

        public async Task<EncryptionResult> ReencryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null)
        {
            var result = EncryptionResult.CreateDefault();

            (string mountPoint, ErrorCode code, string errorMessage) = GetMountPointFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            string encryptionKeyName;
            (encryptionKeyName, code, errorMessage) = GetEncryptionKeyNameFromParameters(parameters);
            if (ErrorCode.None != code)
            {
                result.Code = code;
                result.Message = errorMessage;
                return result;
            }

            var options = new RewrapRequestOptions()
            {
                BatchedRewrapItems = new List<DecryptionItem>()
            };

            foreach (KeyValuePair<string, string> item in kvCipherText)
            {
                var decItem = new DecryptionItem()
                {
                    Base64EncodedContext = item.Key.ToBase64(),
                    CipherText = item.Value
                };
                options.BatchedRewrapItems.Add(decItem);
            }

            Secret<EncryptionResponse> encResponse = await _vaultClient.V1.Secrets.Transit.RewrapAsync(encryptionKeyName, options, mountPoint);

            var kvNewCipherText = new Dictionary<string, string>();
            int position = 0;
            foreach (var batchItem in encResponse.Data.BatchedResults)
            {
                kvNewCipherText.Add(kvCipherText.Keys.ElementAt(position), batchItem.CipherText);
                position++;
            }

            // Record the parameters used in this re-encryption request
            var usedParameters = new Dictionary<string, object>()
                {
                    { CommonParameterNames.KeyScope, mountPoint },
                    { CommonParameterNames.KeyName, encryptionKeyName },
                };

            // TODO: improve this. get the key version from Vault or from the incoming parameters... don't rely on the text returned, it may change in the future
            usedParameters.Add(CommonParameterNames.KeyVersion, GetKeyVersionFromCipherText(kvNewCipherText.First().Value));

            result.RequestId = encResponse.RequestId;
            result.Data = kvNewCipherText;
            result.Parameters = usedParameters;
            return result;
        }

        private int GetKeyVersionFromCipherText(string cipherText)
        {
            string[] parts = cipherText.Split(':'); // the cipher text in the form: "vault:v<version-number>:<base65-text>"
            return int.Parse(parts[1][1..]);
        }

        private (string, ErrorCode, string) GetMountPointFromParameters(Dictionary<string, object> parameters)
        {
            string mountPoint = Parameters.GetValue(parameters, CommonParameterNames.KeyScope, _defaultMountPoint);
            if (string.IsNullOrWhiteSpace(mountPoint))
            {
                return (null, ErrorCode.Crypto_Encryption_Context_Not_Specified, "The encryption key mount point (scope) was not specified explicitly or through configuration.");
            }
            return (mountPoint, ErrorCode.None, null);
        }

        private (string, ErrorCode, string) GetEncryptionKeyNameFromParameters(Dictionary<string, object> parameters)
        {
            string encryptionKeyName = Parameters.GetValue(parameters, CommonParameterNames.KeyName, _defaultKeyName);
            if (string.IsNullOrWhiteSpace(encryptionKeyName))
            {
                return (null, ErrorCode.Crypto_Encryption_Key_Not_Specified, "The encryption key was not specified explicitly or through configuration.");
            }
            return (encryptionKeyName, ErrorCode.None, null);
        }

        private (ErrorCode, string) MapVaultExceptionToErrorCode(VaultApiException e)
        {
            ErrorCode error = ErrorCode.Crypto_Undefined_Error;
            string message = string.Join(" | ", e.ApiErrors);
            if (e.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                error = ErrorCode.Crypto_Access_Denied_To_Key_or_Service;
                message = $"Vault access denied - {message}";
            }
            else if (e.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                error = ErrorCode.Crypto_Service_Not_Available;
                message = $"Vault service is not available - {message}";
            }
            else if (e.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                error = ErrorCode.Crypto_Encryption_Key_Not_Found;
                message = $"Invalid encryption key or mount - {message}";
            }
            else if (e.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                if (message.Contains("invalid username", StringComparison.InvariantCultureIgnoreCase))
                {
                    error = ErrorCode.Crypto_Invalid_Configuration;
                    message = $"Invalid configuration to access Vault service - {message}";
                }
                else if(message.Contains("encryption key not found", StringComparison.InvariantCultureIgnoreCase))
                {
                    error = ErrorCode.Crypto_Encryption_Key_Not_Found;
                    message = $"Orphaned encrypted data - {message}";
                }
            }
            return (error, message);
        }

        public async Task<EncryptionKeyVersionResult> GetEncryptionKeyVersionInfoAsync(string keyName, string keyScope, Dictionary<string, object> parameters = null)
        {
            var result = new EncryptionKeyVersionResult();

            // Make the request to the Vault server to encrypt the data items
            try
            {
                Secret<EncryptionKeyInfo> response = await _vaultClient.V1.Secrets.Transit.ReadEncryptionKeyAsync(keyName, mountPoint: keyScope);
                result.CurrentVersion = int.Parse(response.Data.Keys.OrderByDescending(x => x.Value).First().Key);
                result.MinimumDecryptionVersion = response.Data.MinimumDecryptionVersion;
                result.MinimumEncryptionVersion = response.Data.MinimumEncryptionVersion;
                result.Name = keyName;
            }
            catch (VaultApiException e)
            {
                _log.LogError(e, e.Message);
                (result.Code, result.Message) = MapVaultExceptionToErrorCode(e);
            }
            catch (HttpRequestException e)
            {
                _log.LogError(e, e.Message);
                result.Code = ErrorCode.Network_Connection_Error;
                result.Message = e.Message;
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message);
                result.Code = ErrorCode.Crypto_Undefined_Error;
                result.Message = e.Message;
            }

            return result;
        }
    }
}