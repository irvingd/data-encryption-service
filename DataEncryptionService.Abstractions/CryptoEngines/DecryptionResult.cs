using System;
using System.Collections.Generic;

namespace DataEncryptionService.CryptoEngines
{
    public sealed class DecryptionResult
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
        public Dictionary<string, string> Data {get;set;}

        public static DecryptionResult CreateDefault()
        {
            return new DecryptionResult()
            {
                Data = new Dictionary<string, string>(),
                RequestId = Guid.NewGuid().ToString()
            };
        }
    }
}