using System;

namespace DataEncryptionService.CryptoEngines
{
    public sealed class RotateKeyResult
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }

        public static RotateKeyResult CreateDefault()
        {
            return new RotateKeyResult()
            {
                RequestId = Guid.NewGuid().ToString()
            };
        }
    }
}