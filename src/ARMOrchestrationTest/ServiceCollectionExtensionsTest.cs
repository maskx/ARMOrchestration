using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using maskx.ARMOrchestration.Extensions;
using ARMCreatorTest;
using Microsoft.Extensions.DependencyInjection;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration;

namespace ARMOrchestrationTest
{
    [Trait("c", "ServiceCollectionExtensions")]
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        private void RunHost()
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
                        ConnectionString = TestHelper.ConnectionString,
                        AutoCreate = true
                    };
                    services.UsingARMOrchestration(sqlConfig);
                })
                .Build();
            webHost.RunAsync();

            var client = webHost.Services.GetService<OrchestrationWorkerClient>();
            var instance = webHost.Services.GetService<ARMOrchestrationClient>().Run(new TemplateOrchestrationInput()
            {
                DeploymentId = Guid.NewGuid().ToString("N")
            }).Result;
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    Assert.Equal("1", result.Output);
                    break;
                }
            }
        }
    }
}