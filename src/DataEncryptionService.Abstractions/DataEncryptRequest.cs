using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataEncryptRequest
    {
        private DataEncryptRequest() { }

        public Dictionary<string, string> Data { get; set; }

        public HashSet<string> Tags { get; set; }

        public EncryptRequestOptions Options { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        static public DataEncryptRequest CreateDefault()
        {
            return new DataEncryptRequest()
            {
                Data = new Dictionary<string, string>()
            };
        }
    }
}
