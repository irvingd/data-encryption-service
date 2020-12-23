using System.Threading.Tasks;

namespace DataEncryptionService
{
    public interface IDataEncryptionManager
    {
        Task<DataEncryptResponse> EncryptDataAsync(DataEncryptRequest request);
        Task<DataDecryptResponse> DecryptDataAsync(DataDecryptRequest request);
        Task<DataDeleteResponse> DeleteDataAsync(DataDeleteRequest request);
        Task<RotateEncryptionResponse> RotateEncryptionKeyAsync(RotateEncryptionRequest request);
    }
}
