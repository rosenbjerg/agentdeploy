namespace AgentDeploy.ApplicationHost.ExternalApi.Options
{
    public class ExecutionOptions
    {
        public string Shell { get; set; } = "bash";
        public bool UseWslPath { get; set; } = true;
    }
}