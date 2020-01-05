using ARMCreatorTest.Mock;
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
                   services.Configure<TemplateOrchestrationOptions>((options) =>
                   {
                       options.Database = new TemplateOrchestrationOptions.DatabaseConfig()
                       {
                           HubName = TestHelper.HubName,
                           ConnectionString = TestHelper.ConnectionString
                       };
                       // options.GetCheckPolicyRequestInput = (input) =>
                       //{
                       //    return CreateAsyncRequestInput("MockCommunicationProcessor", input);
                       //};
                       options.GetCreateResourceRequestInput = (input) =>
                      {
                          return TestHelper.CreateAsyncRequestInput("MockCommunicationProcessor", input);
                      };
                   });
               }).Build();
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