using System.Runtime.Serialization;

namespace DataEncryptionService.CLI.Telemetry.Names
{
    internal enum SpanName
    {
        [EnumMember(Value = "Toolaction Completed")]
        Toolaction_Execution_Time = 2000
    }
}