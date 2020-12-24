using System;
using System.Collections.Generic;

namespace DataEncryptionService.CryptoEngines
{
    public sealed class EncryptionResult
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }

        public string RequestId { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public static EncryptionResult CreateDefault()
        {
            return new EncryptionResult()
            {
                RequestId = Guid.NewGuid().ToString()
            };
        }
    }
}