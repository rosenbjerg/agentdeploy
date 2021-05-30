using System.ComponentModel.DataAnnotations;

namespace AgentDeploy.Models.Options
{
    public class DirectoryOptions
    {
        /// <summary>
        /// Directory containing yaml script files
        /// </summary>
        [Required]
        public string Scripts { get; set; } = null!;
        
        /// <summary>
        /// Directory containing yaml token files
        /// </summary>
        [Required]
        public string Tokens { get; set; } = null!;
        
        /// <summary>
        /// Directory containing assets for scripts
        /// </summary>
        [Required]
        public string Assets { get; set; } = null!;
    }
}