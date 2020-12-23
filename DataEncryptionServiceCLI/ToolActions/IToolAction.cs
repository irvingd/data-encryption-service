using System.Threading.Tasks;

namespace DataEncryptionService.CLI.ToolActions
{
    public interface IToolAction
    {
        string Name { get; }
        ToolRunAction Action { get; }
        bool HasValidConfiguration();
        bool HasValidRunOptions(RuntimeOptions options);
        Task ExecuteActionAsync(RuntimeOptions options);
    }
}
