namespace AgentDeploy.Services.TypeValidation
{
    public sealed class MailAddressTypeValidator : ITypeValidator
    {
        public bool IsValid(string content)
        {    
            try {
                var addr = new System.Net.Mail.MailAddress(content);
                return addr.Address == content;
            }
            catch {
                return false;
            }
        }
    }
}