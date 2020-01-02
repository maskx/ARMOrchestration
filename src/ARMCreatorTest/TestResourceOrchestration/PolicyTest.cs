using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace ARMCreatorTest.TestResourceOrchestration
{
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "ICheckPolicy")]
    public class PolicyTest : IDisposable
    {
        private IHost workerHost = null;
        public OrchestrationWorker OrchestrationWorker { get; private set; }

        public PolicyTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            List<Type> orchestrationTypes = new List<Type>();
            List<Type> activityTypes = new List<Type>();
            Dictionary<Type, object> interfaceActivitys = new Dictionary<Type, object>();
            workerHost = TestHelper.CreateHostBuilder(options, orchestrationTypes, activityTypes, interfaceActivitys).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync().Wait();
        }

        [Fact(DisplayName = "NoPolicy")]
        public void NoPolicy()
        {
            TestHelper.OrchestrationTest(this.OrchestrationWorker,
                "Condition/TrueCondition",
                (instance, args) =>
                {
                    return args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId;
                },
                (instance, args) =>
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                });
        }
    }
}