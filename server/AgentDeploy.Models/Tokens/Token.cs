using System.Collections.Generic;

namespace AgentDeploy.Models.Tokens
{
    public class Token
    {
        /// <summary>
        /// Only for own usage. Not used by application
        /// </summary>
        public string? Name { get; init; } = null!;
        
        /// <summary>
        /// Only for own usage. Not used by application
        /// </summary>
        public string? Description { get; init; } = null!;
        
        /// <summary>
        /// Global SSH options for commands executed using token, unless overridden
        /// </summary>
        public SecureShellOptions? Ssh { get; init; }
        
        /// <summary>
        /// Trusted IPs and IP ranges. All IPs are allowed if set to null
        /// </summary>
        public List<string>? TrustedIps { get; init; } = null!;
        
        /// <summary>
        /// Limit access to scripts for token. Optionally with further constraints
        /// </summary>
        public Dictionary<string, ScriptAccessDeclaration?>? AvailableScripts { get; init; } = null!;
    }
}