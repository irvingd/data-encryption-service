using System.Collections.Generic;
using DataEncryptionService.WebApi.Models;

namespace DataEncryptionService.WebApi.Controllers
{
    public static class MappingExtensions
    { 
        public static DataEncryptRequest ToRequest(this ApiDataEncryptRequest apiRequest)
        {
            var request = DataEncryptRequest.CreateDefault();
            foreach (var item in apiRequest.Data)
            {
                request.Data.TryAdd(item.Key, item.Value);
            }
            return request;
        }

        public static DataDecryptRequest ToRequest(this ApiDataDecryptRequest apiRequest)
        {
            var request = DataDecryptRequest.CreateDefault();
            foreach(var item in apiRequest.Items)
            {
                request.LabeledData.Add(new LabeledEncryptedData()
                {
                    Label = item.Label,
                    Items = item.Items
                });
            }
            return request;
        }

        public static DataDeleteRequest ToRequest(this ApiDataDeleteRequest apiRequest)
        {
            var request = DataDeleteRequest.CreateDefault();
            request.Labels.AddRange(apiRequest.Labels);
            return request;
        }

        public static ApiDataEncryptResponse ToApiResponse(this DataEncryptResponse response)
        {
            return new ApiDataEncryptResponse()
            {
                ErrorCode = response.Error != ErrorCode.None ? (int)response.Error : null,
                ErrorMessage = response.Message,
                RequestId = response.RequestId,
                Label = response.Label,
                Items = MapFrom(response.Data)
            };
        }

        public static ApiDataDecryptResponse ToApiResponse(this DataDecryptResponse response)
        {
            return new ApiDataDecryptResponse()
            {
                ErrorCode = response.Error != ErrorCode.None ? (int)response.Error : null,
                ErrorMessage = response.Message,
                RequestId = response.RequestId,
                HasErrors = response.HasErrors,
                LabeledData = MapFrom(response.LabeledData)
            };
        }

        public static ApiDataDeleteResponse ToApiResponse(this DataDeleteResponse response)
        {
            return new ApiDataDeleteResponse()
            {
                ErrorCode = response.Error != ErrorCode.None ? (int)response.Error : null,
                ErrorMessage = response.Message,
                RequestId = response.RequestId,
                HasErrors = response.HasErrors,
                LabelResponses = MapFrom(response.LabelResponses)
            };
        }

        private static List<DecryptedData> MapFrom(List<LabeledDecryptedData> src)
        {
            List<DecryptedData> dst = null;
            if (src?.Count > 0)
            {
                dst = new List<DecryptedData>();
                foreach (var item in src)
                {
                    dst.Add(new DecryptedData()
                    {
                        ErrorCode = item.Error != ErrorCode.None ? (int)item.Error : null,
                        ErrorMessage = item.Message,
                        Label = item.Label,
                        Data = item.Data
                    });
                }
            }

            return dst;
        }

        private static List<EncryptedItem> MapFrom(Dictionary<string, EncryptedValue> src)
        {
            List<EncryptedItem> dst = null;
            if (src?.Count > 0)
            {
                dst = new List<EncryptedItem>();
                foreach (var item in src.Values)
                {
                    dst.Add(new EncryptedItem()
                    {
                        Name = item.Name,
                        Hash = item.Hash
                    });
                }
            }

            return dst;
        }

        private static List<DeleteLabel> MapFrom(List<DeleteLabelResponse> src)
        {
            List<DeleteLabel> dst = null;
            if (src?.Count > 0)
            {
                dst = new();
                foreach (var item in src)
                {
                    dst.Add(new DeleteLabel()
                    {
                        Label = item.Label,
                        ErrorCode = (int)item.Error,
                        ErrorMessage = item.Message
                    });
                }
            }

            return dst;
        }
    }
}
