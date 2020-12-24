using System;
using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataEncryptResponse : BaseResponse
    {
        private DataEncryptResponse() { }

        public string Label { get; set; }

        public Dictionary<string, EncryptedValue> Data { get; private set; }

        static public DataEncryptResponse CreateDefault()
        {
            return new DataEncryptResponse()
            {
                RequestId = Guid.NewGuid().ToString(),
                Data = new Dictionary<string, EncryptedValue>()
            };
        }
    }
}
