using System;
using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataDeleteResponse : BaseResponse
    {
        private DataDeleteResponse() { }

        public List<DeleteLabelResponse> LabelResponses { get; set; }

        public bool HasErrors { get; set; }

        static public DataDeleteResponse CreateDefault()
        {
            return new DataDeleteResponse()
            {
                RequestId = Guid.NewGuid().ToString(),
                LabelResponses = new List<DeleteLabelResponse>()
            };
        }
    }
}
