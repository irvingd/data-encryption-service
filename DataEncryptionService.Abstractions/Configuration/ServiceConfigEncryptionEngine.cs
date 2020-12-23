using System;
using System.Collections.Generic;

namespace DataEncryptionService.Configuration
{
    public class ServiceConfigEncryptionEngine
    {
        public Guid EngineId { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}