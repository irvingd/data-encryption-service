using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DataEncryptionService.CryptoEngines;

namespace DataEncryptionService.Core.CryptoEngines.dotNetCapi
{
    public abstract class BaseLegacyCryptoEngine
    {
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;
        private string _keyName;

        protected void InitializeProvider(SymmetricAlgorithm cryptoProvider, string base64CryptoKey, string base64InitVector, string keyName)
        {
            // Should the default, but we're just being explicit
            cryptoProvider.Mode = CipherMode.CBC;

            byte[] cryptoKey = Convert.FromBase64String(base64CryptoKey);
            byte[] initVector = Convert.FromBase64String(base64InitVector);

            _encryptor = cryptoProvider.CreateEncryptor(cryptoKey, initVector);
            _decryptor = cryptoProvider.CreateDecryptor(cryptoKey, initVector);

            _keyName = keyName;
        }

        public Task<EncryptionResult> EncryptAsync(IReadOnlyDictionary<string, string> kvClearText, Dictionary<string, object> parameters = null)
        {
            var kvCipherText = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> item in kvClearText)
            {
                byte[] inputBuffer = Encoding.ASCII.GetBytes(item.Value);
                byte[] outputBuffer = _encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                kvCipherText.Add(item.Key, Convert.ToBase64String(outputBuffer));
            }
            EncryptionResult result = new EncryptionResult()
            {
                Data = kvCipherText,
                Parameters = new Dictionary<string, object>()
                                    {
                                        { CommonParameterNames.KeyName, _keyName },
                                        { CommonParameterNames.KeyVersion, 1 }, // .NET CAPI engines don't really support "key versioning" like Vault or other SMS (secret management solutions)
                                    },
                RequestId = Guid.NewGuid().ToString()
            };

            return Task.FromResult(result);
        }

        public Task<DecryptionResult> DecryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null)
        {
            var result = DecryptionResult.CreateDefault();
            foreach (KeyValuePair<string, string> item in kvCipherText)
            {
                byte[] inputBuffer = Convert.FromBase64String(item.Value);
                byte[] outputBuffer = _decryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                result.Data.Add(item.Key, Encoding.ASCII.GetString(outputBuffer));
            }

            return Task.FromResult(result);
        }

        public Task<EncryptionResult> ReencryptAsync(IReadOnlyDictionary<string, string> kvCipherText, Dictionary<string, object> parameters = null)
        {
            // .NET CAPI do not support multiple version of encryption keys, so this is no-op
            throw new NotSupportedException(".NET CAPI encryption engines do not support multiple version of encryption keys.");
        }

        public Task<RotateKeyResult> RotateEncryptionKeyAsync(string keyName, Dictionary<string, object> parameters = null)
        {
            // This a NO-OP on the .NET CAPI engines

            return Task.FromResult(new RotateKeyResult());
        }

        public Task<EncryptionKeyVersionResult> GetEncryptionKeyVersionInfoAsync(string keyName, string keyScope, Dictionary<string, object> parameters = null)
        {
            throw new NotImplementedException();
        }
    }
}