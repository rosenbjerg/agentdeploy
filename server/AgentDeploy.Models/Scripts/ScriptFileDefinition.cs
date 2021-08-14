namespace AgentDeploy.Models.Scripts
{
    public class ScriptFileDefinition
    {
        /// <summary>
        /// Minimum required size for file
        /// </summary>
        public long MinSize { get; init; } = 0;
        
        /// <summary>
        /// Maximum required size for file
        /// </summary>
        public long MaxSize { get; init; } = long.MaxValue;
        
        /// <summary>
        /// Whether this file is optional/can be skipped.
        /// The path variable used in scripts will be empty string if file is optional and not provided
        /// </summary>
        public bool Optional { get; init; }
        
        /// <summary>
        /// Array of acceptable file extensions
        /// </summary>
        public string[]? AcceptedExtensions { get; set; }

        /// <summary>
        /// Optional file preprocessing.
        /// Useful for e.g. running virus scan or some file validation
        /// </summary>
        public string? FilePreprocessing { get; init; }
    }
}