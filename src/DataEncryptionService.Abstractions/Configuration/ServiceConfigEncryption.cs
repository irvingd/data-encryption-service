using System;
using System.Collections.Generic;

namespace DataEncryptionService.Configuration
{
    public class ServiceConfigEncryption
    {
        public bool AllowRequestParameters { get; set; } = false;
        public Guid DefaultEngine { get; set; } = WellKnownConstants.DotNet.AesCapi.CryptoEngineUUID;
        public Dictionary<string, string> ActiveKeys { get; set; } = new Dictionary<string, string>();
        public List<ServiceConfigEncryptionKeyConfiguration> KeyConfigurations { get; set; } = new List<ServiceConfigEncryptionKeyConfiguration>();
        public List<ServiceConfigEncryptionEngine> EngineConfigurations { get; set; } = new List<ServiceConfigEncryptionEngine>();
    }
}