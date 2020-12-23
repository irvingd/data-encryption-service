using DataEncryptionService.WebApi.Models;

namespace DataEncryptionService.WebApi.Controllers
{
    public static class Validation
    {
        static public bool IsInvalid(ApiDataEncryptRequest request)
        {
            if (request?.Data?.Count > 0)
            {
                return false;
            }

            return true;
        }

        static public bool IsInvalid(ApiDataDecryptRequest request)
        {
            if (request?.Items?.Count > 0)
            {
                return false;
            }

            return true;
        }

        static public bool IsInvalid(ApiDataDeleteRequest request)
        {
            if (request?.Labels?.Count > 0)
            {
                return false;
            }

            return true;
        }
    }
}
