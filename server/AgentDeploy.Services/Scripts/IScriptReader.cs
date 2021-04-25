using System.Threading.Tasks;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptReader
    {
        Task<Models.Scripts.Script?> Load(string scriptName);
    }
}