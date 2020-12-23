using System.Collections.Generic;

namespace DataEncryptionService
{
    public class DataDeleteRequest
    {
        private DataDeleteRequest() { } 
        public List<string> Labels { get; set; }

        static public DataDeleteRequest CreateDefault()
        {
            return new DataDeleteRequest()
            {
                Labels = new List<string>()
            };
        }
    }
}