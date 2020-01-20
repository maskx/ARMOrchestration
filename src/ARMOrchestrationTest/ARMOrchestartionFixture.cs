using ARMCreatorTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
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
                   services.AddSingleton<ICommunicationProcessor>(new MockCommunicationProcessor());
                   services.Configure<ARMOrchestrationOptions>((options) =>
                   {
                       options.Database = new DatabaseConfig()
                       {
                           HubName = TestHelper.HubName,
                           ConnectionString = TestHelper.ConnectionString
                       };
                       options.GetRequestInput = (sp, cxt, res, name, property) =>
                       {
                           return TestHelper.CreateAsyncRequestInput("MockCommunicationProcessor", res);
                       };
                   });
               }).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
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