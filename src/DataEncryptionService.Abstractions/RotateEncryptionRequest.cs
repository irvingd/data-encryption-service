using System;
using System.Collections.Generic;
using System.Threading;

namespace DataEncryptionService
{
    public class RotateEncryptionRequest
    {
        private RotateEncryptionRequest() { }

        public Guid? EngineId { get; set; }
        public string KeyName { get; set; }
        public string KeyScope { get; set; }
        public CancellationToken CancelToken { get; set; }
        public Action<string, int, ErrorCode, string> ProgressCallback { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string StartingLabel { get; set; }
        public DateTime? FromEncryptedOn { get; set; }

        public static RotateEncryptionRequest CreateDefault(string keyScope, string keyName, CancellationToken token)
        {
            return new RotateEncryptionRequest()
            {
                KeyScope = keyScope,
                KeyName = keyName,
                CancelToken = token,
                EngineId = WellKnownConstants.Vault.CryptoEngineUUID
            };
        }
    }
}