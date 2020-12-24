using System;
using System.Collections.Generic;

namespace DataEncryptionService.Storage
{
    public interface IPersistedSecureData
    {
        DateTime CreatedOn { get; set; }
        List<EncryptedDataItem> Data { get; set; }
        DateTime EncryptedOn { get; set; }
        Dictionary<string, object> EncryptionParameters { get; set; }
        Guid EngineId { get; set; }
        string EngineRequestId { get; set; }
        string HashMethod { get; set; }
        string KeyName { get; set; }
        string KeyScope { get; set; }
        int KeyVersion { get; set; }
        string Label { get; set; }
        HashSet<string> Tags { get; set; }
    }
}