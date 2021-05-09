using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptTransformer
    {
        Task<string> PrepareScriptFile(ScriptInvocationContext invocationContext, string directory);
        string BuildScriptPath(string directory);
        string BuildScriptArgument(string scriptFilePath);
        string EscapeWhitespaceInPath(string path, char escapeChar = '"');
        string HideSecrets(string text, ScriptInvocationContext invocationContext);
    }
}