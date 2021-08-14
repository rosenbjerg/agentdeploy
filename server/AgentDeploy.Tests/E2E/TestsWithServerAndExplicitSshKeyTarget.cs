using System.IO;
using System.Runtime.InteropServices;
using AgentDeploy.Models.Tokens;
using Instances;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E-privatekey")]
    public class TestsWithServerAndExplicitSshKeyTarget : TestsWithServer
    {
        private const string PrivateKeyPath = "E2E/Files/id_rsa";
        public TestsWithServerAndExplicitSshKeyTarget() : base(new SecureShellOptions
        {
            Address = "ssh-target",
            Username = "root",
            PrivateKeyPath = Path.GetFullPath(PrivateKeyPath),
        })
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set permissions on id_rsa
                Instance.FinishAsync("chmod", $"600 {SecureShellOptions!.PrivateKeyPath}");
            }
        }
    }
}