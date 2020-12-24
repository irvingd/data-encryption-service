using System.Collections.Generic;

namespace DataEncryptionService
{
    public class LabeledEncryptedData
    {
        public string Label { get; set; }

        public HashSet<string> Items { get; set; } = new HashSet<string>();
    }
}
