using DurableTask.Core.Serializing;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARMCreatorTest
{
    public class ARMOrchestartionFixture : IDisposable
    {
        private IHost workerHost = null;
        public OrchestrationWorker OrchestrationWorker { get; private set; }

        public ARMOrchestartionFixture()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            List<Type> orchestrationTypes = new List<Type>();
            workerHost = TestHelper.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync().Wait();
        }
    }

    [CollectionDefinition("WebHost ARMOrchestartion")]
    public class WebHostCollection : ICollectionFixture<ARMOrchestartionFixture>
    {
    }
}