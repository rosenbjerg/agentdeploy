namespace AgentDeploy.Models.Tokens
{
    public class SecureShellOptions
    {
        public string Username { get; init; } = null!;
        public string? Password { get; init; }
        public string Address { get; init; } = "host.docker.internal";
        public int Port { get; init; } = 22;
        
        /// <summary>
        /// Directory to use as root for remote execution of scripts using SSH remote execution
        /// </summary>
        public string TemporaryAgentDirectory { get; init; } = "/tmp";
        
        public string? PrivateKeyPath { get; init; }

        /// <summary>
        /// Whether to require strict host key checking when connecting using SSH
        /// </summary>
        public HostKeyCheckingOptions HostKeyChecking { get; init; } = HostKeyCheckingOptions.AcceptNew;
    }
}