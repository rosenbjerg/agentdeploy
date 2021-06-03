using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Services.Scripts;
using AgentDeploy.Services.TypeValidation;

namespace AgentDeploy.Services
{
    public sealed class InvocationContextService : IInvocationContextService
    {
        private readonly IOperationContext _operationContext;
        private readonly IScriptReader _scriptReader;
        private readonly ITypeValidationService _typeValidationService;

        public InvocationContextService(IOperationContext operationContext, IScriptReader scriptReader, ITypeValidationService typeValidationService)
        {
            _operationContext = operationContext;
            _scriptReader = scriptReader;
            _typeValidationService = typeValidationService;
        }
        
        public async Task<ScriptInvocationContext?> Build(ParsedScriptInvocation scriptInvocation)
        {
            if (!HasAccessToScript(scriptInvocation.ScriptName))
                return null;

            var script = await _scriptReader.Load(scriptInvocation.ScriptName, _operationContext.OperationCancelled);
            if (script == null)
                return null;

            var scriptAccessDeclaration = _operationContext.Token.AvailableScripts?[scriptInvocation.ScriptName];
            var (acceptedVariables, acceptedFiles) = _typeValidationService.Validate(scriptInvocation, script, scriptAccessDeclaration);

            return new ScriptInvocationContext
            {
                Script = script,
                Arguments = acceptedVariables,
                Files = acceptedFiles.ToArray(),
                EnvironmentVariables = scriptInvocation.EnvironmentVariables,
                SecureShellOptions = scriptAccessDeclaration?.Ssh ?? _operationContext.Token.Ssh,
                WebSocketSessionId = scriptInvocation.WebsocketSessionId,
                CorrelationId = _operationContext.CorrelationId
            };
        }


        private bool HasAccessToScript(string scriptName)
        {
            return _operationContext.Token.AvailableScripts == null || _operationContext.Token.AvailableScripts.ContainsKey(scriptName);
        }
    }
}