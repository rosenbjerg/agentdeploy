using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E")]
    public class TestsWithServerAndLocalTarget : TestsWithServer
    {
        public TestsWithServerAndLocalTarget() : base(null)
        {
        }
    }
}