﻿using ARMOrchestrationTest.Mock;
using DurableTask.Core;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ARMOrchestrationTest.PluginTests
{
    [Trait("c", "PluginTest")]
    public class PluginTest
    {
        private readonly IServiceProvider serviceProvider;

        public PluginTest()
        {
            List<(string, string, Type)> orchestrations = new List<(string, string, Type)>()
            {
                (typeof(BeforeDeploymentOrchestration).FullName,"",typeof(BeforeDeploymentOrchestration)),
                (typeof(AfterDeploymentOrhcestration).FullName,"",typeof(AfterDeploymentOrhcestration)),
                (typeof(BeforeResourceProvisioningOrchestation).FullName,"",typeof(BeforeResourceProvisioningOrchestation)),
                (typeof(AfterResourceProvisioningOrchestation).FullName,"",typeof(AfterResourceProvisioningOrchestation))
            };
            var host = TestHelper.CreateHostBuilder(null, orchestrations, null, null, null).Build();
            host.RunAsync();
            this.serviceProvider = host.Services;
            var infra = this.serviceProvider.GetService<IInfrastructure>();
            infra.BeforeDeploymentOrchestration.Add((typeof(BeforeDeploymentOrchestration).FullName, ""));
            infra.AfterDeploymentOrhcestration.Add((typeof(AfterDeploymentOrhcestration).FullName, ""));
            infra.BeforeResourceProvisioningOrchestation.Add((typeof(BeforeResourceProvisioningOrchestation).FullName, ""));
            infra.AfterResourceProvisioningOrchestation.Add((typeof(AfterResourceProvisioningOrchestation).FullName, ""));
        }

        [Fact(DisplayName = "Test")]
        public void Test()
        {
            var client = serviceProvider.GetService<OrchestrationWorkerClient>();
            var instance = serviceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>().Run(
                new Deployment()
                {
                    ApiVersion = "1.0",
                    Name = "UsingARMOrchestrationTest",
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    Template = TestHelper.GetJsonFileContent("PluginTests/NestTemplate"),
                    SubscriptionId = Guid.NewGuid().ToString(),
                    ResourceGroup = TestHelper.ResourceGroup,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    TenantId = "TenantId",
                    CreateByUserId = TestHelper.CreateByUserId
                }).Result;
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(new OrchestrationInstance() { ExecutionId = instance.ExecutionId, InstanceId = instance.InstanceId }, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, r.Code);
                    var r1 = JObject.Parse(r.Content.ToString());
                    var r2 = r1["properties"]["outputs"] as JObject;
                    Assert.True(r2.ContainsKey("BeforeDeploy"));
                    Assert.Equal("BeforeDeploymentOrchestration", r2["BeforeDeploy"]["value"].ToString());
                    Assert.True(r2.ContainsKey("AfterDeploy"));
                    Assert.Equal("AfterDeploymentOrhcestration", r2["AfterDeploy"]["value"].ToString());
                    break;
                }
            }
        }
    }
}