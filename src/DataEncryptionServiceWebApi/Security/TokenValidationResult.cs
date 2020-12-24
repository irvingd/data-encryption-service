namespace DataEncryptionService.WebApi.Security
{
    public class TokenValidationResult
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string EntityId { get; set; }
    }
}