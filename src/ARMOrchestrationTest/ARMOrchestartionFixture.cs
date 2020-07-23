using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace ARMOrchestrationTest
{
    public class ARMOrchestartionFixture 
    {
        private IHost workerHost = null;
        public IServiceProvider ServiceProvider { get; private set; }
        public OrchestrationWorker OrchestrationWorker { get; private set; }
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }
        public ARMFunctions ARMFunctions { get; set; }
        public ARMOrchestrationClient ARMOrchestrationClient { get; private set; }

        public ARMOrchestartionFixture()
        {
            workerHost = TestHelper.CreateHostBuilder(null).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            this.ARMFunctions = workerHost.Services.GetService<ARMFunctions>();
            this.ARMOrchestrationClient = workerHost.Services.GetService<ARMOrchestrationClient>();
            this.ServiceProvider = workerHost.Services;
        }
    }

    [CollectionDefinition("WebHost ARMOrchestartion")]
    public class WebHostCollection : ICollectionFixture<ARMOrchestartionFixture>
    {
    }
}