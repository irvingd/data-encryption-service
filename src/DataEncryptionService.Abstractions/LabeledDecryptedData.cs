using System.Collections.Generic;

namespace DataEncryptionService
{
    public class LabeledDecryptedData
    {
        public ErrorCode Error { get; set; }

        public string Message { get; set; }

        public string Label { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public static LabeledDecryptedData CreateDefault(string label, Dictionary<string, string> data)
        {
            return new LabeledDecryptedData()
            {
                Label = label,
                Data = data
            };
        }

        public static LabeledDecryptedData CreateForError(ErrorCode errorCode, string errorMessage = null)
        {
            return new LabeledDecryptedData()
            {
                Error = errorCode,
                Message = errorMessage
            };
        }
    }
}
