using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataDecryptRequest
    {
        private DataDecryptRequest() { }

        public List<LabeledEncryptedData> LabeledData { get; private set; }

        static public DataDecryptRequest CreateDefault()
        {
            return new DataDecryptRequest()
                {
                    LabeledData = new List<LabeledEncryptedData>()
                };
        }
    }
}
