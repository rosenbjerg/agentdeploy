using System.IO;

namespace AgentDeploy.Services
{
    public static class WslUtils
    {
        public static string TransformPath(string path)
        {
            return Path.GetFullPath(path).Replace("C:\\", "/mnt/c/").Replace("\\", "/");
        }
    }
}