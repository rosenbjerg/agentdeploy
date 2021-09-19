using AgentDeploy.Services;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class PathUtilsTests
    {
        [TestCase("/tmp/agentd/", '/', "/tmp", "agentd/")]
        [TestCase("/tmp/agentd", '/', "/tmp", "agentd")]
        [TestCase("tmp/agentd", '/', "tmp", "agentd")]
        [TestCase("C:\\Test", '\\', "C:", "Test")]
        public void CombineTests(string expectedResult, char separator, params string[] pathParts)
        {
            var actualResult = PathUtils.Combine(separator, pathParts);
            Assert.AreEqual(expectedResult, actualResult);
        }
        
        [TestCase("/bin/bash", "/bin/bash", '\'')]
        [TestCase("$/bin/bash/My Dir/file$", "/bin/bash/My Dir/file", '$')]
        [TestCase("'/bin/bash/My Dir/file'", "/bin/bash/My Dir/file", '\'')]
        [TestCase("/bin/bash/my_dir/file", "/bin/bash/my_dir/file", '\'')]
        public void EscapeWhitespaceInPath(string expectedResult, string path, char escapeChar)
        {
            var actualResult = PathUtils.EscapeWhitespaceInPath(path, escapeChar);
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}