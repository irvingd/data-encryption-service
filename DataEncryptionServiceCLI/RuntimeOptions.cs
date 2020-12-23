using System;
using CommandLine;
using DataEncryptionService.CLI.ToolActions;

namespace DataEncryptionService.CLI
{
    public class RuntimeOptions
    {
        [Option('a', "action", Required = true, HelpText = "Specific action that the tool will execute. Valid options are:\r\n    RotateKey (int: 1)\r\n    GenerateAesKey (int: 2)\r\n    Generate3desKey (int: 3)")]
        public ToolRunAction Action { get; set; }

        [Option('s', "keyscope" , HelpText = "The scope of the encryption key.")]
        public string KeyScope { get; set; }

        [Option('n', "keyname", HelpText = "The name of the encryption key.")]
        public string KeyName { get; set; }

        [Option('l', "FromLabel", HelpText = "Starting label to being the key rotation and re-encryption from.")]
        public string StartingLabel { get; set; }

        [Option('e', "FromEncryptedOn", HelpText = "Starting encryption date to begin the key rotation and re-encryption from.")]
        public DateTime? FromEncryptedOn { get; set; }

        [Option('q', "Quiet", HelpText = "Suppress any verbose text to the console.")]
        public bool Quiet { get; set; }
    }
}