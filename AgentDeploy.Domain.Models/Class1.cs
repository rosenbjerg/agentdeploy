using System;
using System.IO;
using System.Threading.Tasks;

namespace AgentDeploy.Domain.Models
{
    public class Profile
    {
        public string Key { get; set; }
        
    }

    public interface IDeployJob
    {
        public string Name => GetType().Name.Replace("DeployJob", string.Empty);
    }

    public class CommandLineDeployJob : IDeployJob
    {
        public string Command { get; set; }
    }
    public class CopyFilesDeployJob : IDeployJob
    {
        public string Files { get; set; }
    }

    public class DockerComposeDeployJob : IDeployJob
    {
        public string Directory { get; set; }
        public string ComposeFileContent { get; set; }
    }
    
}