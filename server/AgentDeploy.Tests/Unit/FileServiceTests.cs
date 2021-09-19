using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class FileServiceTests
    {
        [TestCase(".yml", true)]
        [TestCase(".yaml", true)]
        [TestCase(".toml", false)]
        public async Task FindFilesTest(string fileExtension, bool expectedToBeFound)
        {
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/'
            };

            var directory = Path.GetTempPath();
            var tempFile = Path.Combine(directory, $"test_file{fileExtension}");
            await File.WriteAllTextAsync(tempFile, "test");
            
            var service = new FileService(executionOptions);
            
            try
            {
                var foundFile = service.FindFile(directory, "test_file", "yml", "yaml");

                if (expectedToBeFound)
                {
                    Assert.NotNull(foundFile);
                    Assert.AreEqual(fileExtension, Path.GetExtension(tempFile));
                }
                else
                {
                    Assert.IsNull(foundFile);
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        
        [Test]
        public async Task ReadAsyncTest_FileExists()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_file.ext");
            await File.WriteAllTextAsync(tempFile, "test");
            
            var service = new FileService(null!);
            
            try
            {
                var fileContent = await service.ReadAsync(tempFile, CancellationToken.None);
                Assert.NotNull(fileContent);
                Assert.AreEqual(fileContent, "test");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        
        [Test]
        public async Task ReadAsyncTest_NotFound()
        {
            var service = new FileService(null!);
            
            var fileContent = await service.ReadAsync("/path/that/does/not/exist/what/so/ever", CancellationToken.None);
            Assert.IsNull(fileContent);
        }
    }
}