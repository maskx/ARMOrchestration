using ARMOrchestrationTest;
using ARMOrchestrationTest.Mock;
using DurableTask.Core;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace ARMOrchestrationTest
{
    [Trait("c", "ServiceCollectionExtensions")]
    public class ServiceCollectionExtensionsTest
    {
        [Fact(DisplayName = "UsingARMOrchestration")]
        public void UsingARMOrchestration()
        {
            var webHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets("afab7740-fb18-44a0-9f16-b94c3327da7e");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var sqlConfig = new ARMOrchestrationSqlServerConfig()
                    {
                        Database = new DatabaseConfig()
                        {
                            ConnectionString = TestHelper.ConnectionString,
                            AutoCreate = true
                        }
                    };
                    services.UsingARMOrchestration(sqlConfig);
                    services.AddSingleton<ICommunicationProcessor>((sp) =>
                    {
                        return new MockCommunicationProcessor();
                    });
                    services.AddSingleton<IInfrastructure>((sp) =>
                    {
                        return new MockInfrastructure(sp);
                    });
                })
                .Build();
            webHost.RunAsync();

            var client = webHost.Services.GetService<OrchestrationWorkerClient>();
            var instance = webHost.Services.GetService<ARMOrchestrationClient>().Run(
                new DeploymentOrchestrationInput()
                {
                    ApiVersion = "1.0",
                    DeploymentName = "UsingARMOrchestrationTest",
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    Template = TestHelper.GetTemplateContent("dependsOn/OneResourceName"),
                    SubscriptionId = TestHelper.SubscriptionId,
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
                var result = client.WaitForOrchestrationAsync(new OrchestrationInstance() { InstanceId = instance.InstanceId, ExecutionId = instance.ExecutionId }, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, r.Code);
                    break;
                }
            }
        }

        [Fact(DisplayName = "UsingARMOrchestration_ConfigFunc")]
        public void UsingARMOrchestration_ConfigFunc()
        {
            var webHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets("afab7740-fb18-44a0-9f16-b94c3327da7e");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.UsingARMOrchestration((sp) =>
                    {
                        return new ARMOrchestrationSqlServerConfig()
                        {
                            Database = new DatabaseConfig()
                            {
                                ConnectionString = TestHelper.ConnectionString,
                                AutoCreate = true
                            }
                        };
                    });
                    services.AddSingleton<ICommunicationProcessor>((sp) =>
                    {
                        return new MockCommunicationProcessor();
                    });
                    services.AddSingleton<IInfrastructure>((sp) =>
                    {
                        return new MockInfrastructure(sp);
                    });
                })
                .Build();
            webHost.RunAsync();

            var client = webHost.Services.GetService<OrchestrationWorkerClient>();
            var instance = webHost.Services.GetService<ARMOrchestrationClient>().Run(
                new DeploymentOrchestrationInput()
                {
                    ApiVersion = "1.0",
                    DeploymentName = "UsingARMOrchestrationTest",
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    Template = TestHelper.GetTemplateContent("dependsOn/OneResourceName"),
                    SubscriptionId = TestHelper.SubscriptionId,
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
                var result = client.WaitForOrchestrationAsync(new OrchestrationInstance() { InstanceId = instance.InstanceId, ExecutionId = instance.ExecutionId }, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, r.Code);
                    break;
                }
            }
        }
    }
}