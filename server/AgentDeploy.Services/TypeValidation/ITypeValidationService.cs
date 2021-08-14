using System.Collections.Generic;
using AgentDeploy.Models;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Services.TypeValidation
{
    public interface ITypeValidationService
    {
        (List<AcceptedScriptInvocationArgument> AcceptedVariables, List<AcceptedScriptInvocationFile> AcceptedFiles) Validate(ParsedScriptInvocation scriptInvocation, Script script, ScriptAccessDeclaration? scriptAccessDeclaration);
    }
}