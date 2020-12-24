using System;

namespace DataEncryptionService
{
    public class RotateEncryptionResponse : BaseResponse
    {
        private RotateEncryptionResponse() { }

        static public RotateEncryptionResponse CreateDefault()
        {
            return new RotateEncryptionResponse()
            {
                RequestId = Guid.NewGuid().ToString()
            };
        }
    }
}
