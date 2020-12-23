namespace DataEncryptionService.WebApi.Models
{
    public sealed class ApiErrorResponse : ApiBaseResponse
    {
        public static ApiErrorResponse FromResponse(ApiBaseResponse response)
        {
            return new ApiErrorResponse()
            {
                ErrorMessage = response.ErrorMessage,
                ErrorCode = response.ErrorCode,
                RequestId = response.RequestId
            };
        }
    }
}
