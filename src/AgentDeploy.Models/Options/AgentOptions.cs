namespace AgentDeploy.Models.Options
{
    public class AgentOptions
    {
        /// <summary>
        /// Whether to trust X-Forwarded-* headers from reverse-proxies
        /// </summary>
        public bool TrustXForwardedHeaders { get; set; } = false;

        /// <summary>
        /// Whether to enable CORS policy for allowing CORS requests (non-localhost)
        /// </summary>
        public bool AllowCors { get; set; } = true;
    }
}