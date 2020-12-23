namespace DataEncryptionService
{
    public abstract class BaseResponse
    {
        public ErrorCode Error { get; set; }

        public string Message { get; set; }

        public string RequestId { get; set; }
    }
}
