using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataEncryptionService.CLI.ToolActions
{
    public class GenerateAesKeyToolAction : IToolAction
    {
        public string Name => "Generate AES Encryption Key";

        public ToolRunAction Action => ToolRunAction.GenerateAesKey;

        public Task ExecuteActionAsync(RuntimeOptions options)
        {
            using (var myAes = new AesCryptoServiceProvider())
            {
                var key = myAes.Key;
                var initializationVector = myAes.IV;

                string text_key = Convert.ToBase64String(key);
                string text_iv = Convert.ToBase64String(initializationVector);

                Console.WriteLine("==========================================================================================");
                Console.WriteLine($"AES Key: {text_key}");
                Console.WriteLine($"AES IV: {text_iv}");
                Console.WriteLine("==========================================================================================");
            }

            return Task.CompletedTask;
        }

        public bool HasValidConfiguration()
        {
            return true;
        }

        public bool HasValidRunOptions(RuntimeOptions options)
        {
            return true;
        }
    }
}
