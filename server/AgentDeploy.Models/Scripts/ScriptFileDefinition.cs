using System;

namespace AgentDeploy.Models.Scripts
{
    public class ScriptFileDefinition
    {
        public long MinSize { get; init; } = 0;
        public long MaxSize { get; init; } = long.MaxValue;
        
        /// <summary>
        /// Array of acceptable file extensions
        /// </summary>
        public string[]? AcceptedExtensions { get; init; }

        public string? FilePreprocessing { get; init; }
    }
}