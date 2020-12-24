namespace DataEncryptionService
{
    public class DeleteLabelResponse
    {
        private DeleteLabelResponse() { }

        public ErrorCode Error { get; set; }

        public string Message { get; set; }

        public string Label { get; set; }

        public static DeleteLabelResponse Create(string label, ErrorCode errorCode = 0, string errorMessage = null)
        {
            return new DeleteLabelResponse()
            {
                Label = label,
                Error = errorCode,
                Message = errorMessage
            };
        }
    }
}
