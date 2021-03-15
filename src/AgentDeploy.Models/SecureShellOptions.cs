namespace AgentDeploy.Models
{
    public class SecureShellOptions
    {
        public string Username { get; set; } = null!;
        public string? Password { get; set; }
        public string Address { get; set; } = "host.docker.internal";
        public int Port { get; set; } = 22;
        public string TemporaryAgentDirectory { get; set; } = "/tmp";
        public string? PrivateKeyPath { get; set; }
        public bool StrictHostKeyChecking { get; set; }
    }
}