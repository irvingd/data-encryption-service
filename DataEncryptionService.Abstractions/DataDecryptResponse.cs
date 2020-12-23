using System;
using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataDecryptResponse : BaseResponse
    {
        private DataDecryptResponse() { }

        public List<LabeledDecryptedData> LabeledData { get; set; }

        public bool HasErrors { get; set; }

        static public DataDecryptResponse CreateDefault()
        {
            return new DataDecryptResponse()
            {
                RequestId = Guid.NewGuid().ToString(),
                LabeledData = new List<LabeledDecryptedData>()
            };
        }
    }
}
