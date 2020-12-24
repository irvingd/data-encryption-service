using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataEncryptionService.CLI.ToolActions
{
    public class Generate3desKeyToolAction : IToolAction
    {
        public string Name => "Generate 3DES Encryption Key";

        public ToolRunAction Action => ToolRunAction.Generate3desKey;

        public Task ExecuteActionAsync(RuntimeOptions options)
        {
            using (var my3des = new TripleDESCryptoServiceProvider())
            {
                var key = my3des.Key;
                var initializationVector = my3des.IV;

                string text_key = Convert.ToBase64String(key);
                string text_iv = Convert.ToBase64String(initializationVector);

                Console.WriteLine("==========================================================================================");
                Console.WriteLine($"3DES Key: {text_key}");
                Console.WriteLine($"3DES IV: {text_iv}");
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
