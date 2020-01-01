﻿using ARMCreatorTest.Mock;
using maskx.OrchestrationCreator;
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
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            List<Type> orchestrationTypes = new List<Type>();
            List<Type> activityTypes = new List<Type>();
            Dictionary<Type, object> interfaceActivitys = new Dictionary<Type, object>();
            interfaceActivitys.Add(typeof(IARMPolicy), new MockARMPolicy());
            interfaceActivitys.Add(typeof(IQuota), new MockQuota());
            interfaceActivitys.Add(typeof(IResource), new MockResource());
            workerHost = TestHelper.CreateHostBuilder(options,
                orchestrationTypes,
                activityTypes,
                interfaceActivitys,
                (hostContext, services) =>
                {
                    services.AddSingleton<ICommunicationProcessor>(new MockCommunicationProcessor());
                    services.Configure<ResourceOrchestrationOptions>((options) =>
                    {
                        options.RPCommunicationProcessorName = "MockCommunicationProcessor";
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