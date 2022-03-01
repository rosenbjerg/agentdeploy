using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ScriptInvocationFileServiceTests
    {
        // [Test]
        // public async Task ReadAsyncTest_FileExists()
        // {
        //     var expectedContent = "test";
        //     var tempFile = Path.Combine(Path.GetTempPath(), $"test_file.ext");
        //     await File.WriteAllTextAsync(tempFile, expectedContent);
        //
        //     var executionOptions = new ExecutionOptions{  };
        //     IScriptInvocationFileService service = new ScriptInvocationFileService();
        //     
        //     try
        //     {
        //         var fileContent = await service.ReadAsync(tempFile, CancellationToken.None);
        //         Assert.NotNull(fileContent);
        //         Assert.AreEqual(expectedContent, fileContent);
        //     }
        //     finally
        //     {
        //         File.Delete(tempFile);
        //     }
        // }
        
    }
}