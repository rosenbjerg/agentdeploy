using System.Threading.Tasks;

namespace AgentDeploy.Services.Script
{
    public interface IScriptReader
    {
        Task<Models.Scripts.Script?> Load(string scriptName);
    }
}