using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataEncryptionService.CLI.ToolActions
{
    public class RotateKeyToolAction : IToolAction
    {
        private ILogger _log;
        private IDataEncryptionManager _dataManager;
        public RotateKeyToolAction(ILogger<RotateKeyToolAction> log, IDataEncryptionManager dataManager)
        {
            _log = log;
            _dataManager = dataManager;
        }

        public string Name => "Rotate Encryption Key";

        public ToolRunAction Action => ToolRunAction.RotateKey;

        public async Task ExecuteActionAsync(RuntimeOptions options)
        {
            // TODO: add support for CTRL+C to end log run

            RotateEncryptionRequest request = RotateEncryptionRequest.CreateDefault(options.KeyScope, options.KeyName, CancellationToken.None);
            if (!options.Quiet)
            {
                request.ProgressCallback = RotateKeyProgressCallback;
            }

            RotateEncryptionResponse response = await _dataManager.RotateEncryptionKeyAsync(request);
            if (ErrorCode.None != response.Error)
            {
                _log.LogError($"And error ocurred while processing the key rotation and re-encryption. [{response.Message} - {response.Message}]", response.RequestId);
            }
        }

        static void RotateKeyProgressCallback(string label, int oldKeyVersion, ErrorCode error, string errorMessage)
        {
            Console.WriteLine($"Finished Processing Label {label} from version {oldKeyVersion}. Error Code: {error}, Error Message: {errorMessage}");
        }

        public bool HasValidConfiguration()
        {
            return true;
        }

        public bool HasValidRunOptions(RuntimeOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.KeyScope) ||
                string.IsNullOrWhiteSpace(options.KeyName))
            {
                return false;
            }
            return true;
        }
    }
}
