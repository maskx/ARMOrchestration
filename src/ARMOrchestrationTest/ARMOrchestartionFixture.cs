using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace ARMCreatorTest
{
    public class ARMOrchestartionFixture : IDisposable
    {
        private IHost workerHost = null;
        public OrchestrationWorker OrchestrationWorker { get; private set; }
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }
        public ARMFunctions ARMFunctions { get; set; }

        public ARMOrchestartionFixture()
        {
            List<Type> orchestrationTypes = new List<Type>();
            List<Type> activityTypes = new List<Type>();
            Dictionary<Type, object> interfaceActivitys = new Dictionary<Type, object>();
            workerHost = TestHelper.CreateHostBuilder(null,
               orchestrationTypes,
               activityTypes,
               interfaceActivitys,
               (hostContext, services) =>
               {
                   services.AddSingleton<IInfrastructure>(new MockInfrastructure());

                   services.AddSingleton<ICommunicationProcessor>(new MockCommunicationProcessor());
                   services.Configure<ARMOrchestrationOptions>((options) =>
                   {
                       options.Database = new DatabaseConfig()
                       {
                           HubName = TestHelper.HubName,
                           ConnectionString = TestHelper.ConnectionString
                       };
                   });
               }).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            this.ARMFunctions = workerHost.Services.GetService<ARMFunctions>();
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