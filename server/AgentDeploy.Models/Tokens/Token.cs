using System.Collections.Generic;

namespace AgentDeploy.Models.Tokens
{
    public class Token
    {
        public string? Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        
        /// <summary>
        /// Global SSH options for commands executed using token, unless overridden
        /// </summary>
        public SecureShellOptions? Ssh { get; set; }
        
        /// <summary>
        /// Trusted IPs and IP ranges. All IPs are allowed if set to null
        /// </summary>
        public List<string>? TrustedIps { get; set; } = null!;
        
        /// <summary>
        /// Limit access to scripts for token. Optionally with further contraints
        /// </summary>
        public Dictionary<string, ScriptAccessDeclaration>? AvailableCommands { get; set; } = null!;
    }
}