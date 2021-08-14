namespace AgentDeploy.Services.TypeValidation
{
    public sealed class NoopValidator : ITypeValidator
    {
        public bool IsValid(string content) => true;
    }
}