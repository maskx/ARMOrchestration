using ARMOrchestrationTest.Mock;
using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ARMOrchestrationTest
{
    public static class TestHelper
    {
        public static void WriteMock(string path, string name, string contents)
        {
            var p = Path.Combine(AppContext.BaseDirectory, "mock", path);
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);
            var f = Path.Combine(p, $"{name}.json");
            File.WriteAllText(f, contents);
        }

        public static string ReadMock(string path)
        {
            var p = Path.Combine(AppContext.BaseDirectory, "mock", $"{path}.json");
            return File.ReadAllText(p);
        }

        public static IConfigurationRoot Configuration { get; private set; }
        public static DataConverter DataConverter { get; private set; } = new JsonDataConverter();
        public static string SubscriptionId = "C1FA36C2-4D58-45E8-9C51-498FADB4D8BF";
        public static string ManagemntGroupId = "E79B1B9F-92CB-4325-9DA5-B3C82C43B6B6";
        public static string ResourceGroup = "ResourceGroup1";
        public static string CreateByUserId = "bob@163.com";
        public static string TenantId = "000";

        public static string ConnectionString
        {
            get
            {
                return Configuration.GetConnectionString("dbConnection");
            }
        }

        static TestHelper()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("afab7740-fb18-44a0-9f16-b94c3327da7e")
                .Build();
        }

        public static string GetFunctionInputContent(string filename)
        {
            string s = Path.Combine(AppContext.BaseDirectory, "TestARMFunctions/json", $"{filename}.json");
            return File.ReadAllText(s);
        }

        public static string GetJsonFileContent(string filename)
        {
            string s = Path.Combine(AppContext.BaseDirectory, $"{filename}.json");
            return JObject.Parse(File.ReadAllText(s)).ToString(Newtonsoft.Json.Formatting.None);
        }

        public static string GetTemplateContent(string filename)
        {
            string s = Path.Combine(AppContext.BaseDirectory, "Templates", $"{filename}.json");
            return File.ReadAllText(s);
        }

        public static string GetParameterContent(string filename)
        {
            string s = Path.Combine(AppContext.BaseDirectory, "Parameters", $"{filename}.json");
            return File.ReadAllText(s);
        }

        public static string GetNodeStringValue(string filename, string path)
        {
            var templatString = TestHelper.GetFunctionInputContent(filename);
            return new JsonValue(templatString).GetNodeStringValue(path);
        }

        public static OrchestrationInstance FunctionTest(
            ARMOrchestartionFixture fixture,
            string filename,
            Dictionary<string, string> result,
            string subscriptionId = null,
            string managementGroupId = null,
            bool usingLinkTemplate = false)
        {
            if (string.IsNullOrEmpty(subscriptionId)) subscriptionId = Guid.NewGuid().ToString();
            var (instance, r) = FunctionTestNotCheckResult(fixture, filename, subscriptionId, managementGroupId: managementGroupId, usingLinkTemplate: usingLinkTemplate);
            var taskResult = TestHelper.DataConverter.Deserialize<TaskResult>(r.Output);
            Assert.Equal(200, taskResult.Code);
            var outputString = taskResult.Content.ToString();
            var templateString = TestHelper.GetFunctionInputContent(filename);
            using var templateDoc = JsonDocument.Parse(templateString);
            using var outputDoc = JsonDocument.Parse(outputString);
            var outputRoot = outputDoc.RootElement.GetProperty("properties").GetProperty("outputs");
            if (templateDoc.RootElement.TryGetProperty("outputs", out JsonElement outputDefineElement))
            {
                List<string> child = new List<string>();
                foreach (var item in outputDefineElement.EnumerateObject())
                {
                    Assert.True(outputRoot.TryGetProperty(item.Name, out JsonElement o), $"cannot find {item.Name} in output");
                    Assert.True(o.TryGetProperty("type", out JsonElement _type));
                    Assert.True(o.TryGetProperty("value", out JsonElement v));

                    if (v.ValueKind == JsonValueKind.String)
                        Assert.True(result[item.Name] == v.GetString(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetString()}");
                    else
                        Assert.True(result[item.Name] == v.GetRawText(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetRawText()}");
                }
            }
            return instance;
        }

        public static (OrchestrationInstance, OrchestrationState) FunctionTestNotCheckResult(
            ARMOrchestartionFixture fixture,
            string filename,
            string subscriptionId,
            string managementGroupId = null, bool usingLinkTemplate = false
)
        {
            Deployment deployInput;
            if (usingLinkTemplate)
                deployInput = new Deployment()
                {
                    TemplateLink = new maskx.ARMOrchestration.ARMTemplate.TemplateLink() { Uri = $"TestARMFunctions/json/{filename}" },
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = string.IsNullOrEmpty(managementGroupId) ? subscriptionId : null,
                    ManagementGroupId = managementGroupId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = TestHelper.TenantId,
                    DeploymentId = Guid.NewGuid().ToString("N")
                };
            else
                deployInput = new Deployment()
                {
                    Template = TestHelper.GetFunctionInputContent(filename),
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = string.IsNullOrEmpty(managementGroupId) ? subscriptionId : null,
                    ManagementGroupId = managementGroupId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = TestHelper.TenantId,
                    DeploymentId = Guid.NewGuid().ToString("N")
                };
            return FunctionTestNotCheckResult(fixture, deployInput);
        }

        private static (OrchestrationInstance, OrchestrationState) FunctionTestNotCheckResult(ARMOrchestartionFixture fixture, Deployment deployInput)
        {
            var deployment = fixture.ARMOrchestrationClient.Run(deployInput).Result;
            var instance = new OrchestrationInstance() { InstanceId = deployment.InstanceId, ExecutionId = deployment.ExecutionId };
            var taskHubClient = new TaskHubClient(fixture.ServiceProvider.GetService<IOrchestrationServiceClient>());
            OrchestrationState result = null;
            while (result == null)
            {
                result = taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
            }
            return (instance, result);
        }

        public static IHostBuilder CreateHostBuilder(
            CommunicationWorkerOptions communicationWorkerOptions = null,
            List<(string, string, Type)> orchestrationTypes = null,
            List<(string, string, Type)> activityTypes = null,
            IDictionary<Type, (string, object)> interfaceActivitys = null,
            Action<HostBuilderContext, IServiceCollection> config = null,
            IInfrastructure infrastructure = null)
        {
            return Host.CreateDefaultBuilder()
             .ConfigureAppConfiguration((hostingContext, config) =>
             {
                 config
                 .AddJsonFile("appsettings.json", optional: true)
                 .AddUserSecrets("afab7740-fb18-44a0-9f16-b94c3327da7e");
             })
             .ConfigureServices((hostContext, services) =>
             {
                 config?.Invoke(hostContext, services);
                 CommunicationWorkerOptions options = new CommunicationWorkerOptions
                 {
                     AutoCreate = true
                 };
                 if (communicationWorkerOptions != null)
                 {
                     options.HubName = communicationWorkerOptions.HubName;
                     options.MaxConcurrencyRequest = communicationWorkerOptions.MaxConcurrencyRequest;

                     options.SchemaName = communicationWorkerOptions.SchemaName;
                 }
                 var sqlConfig = new ARMOrchestrationSqlServerConfig()
                 {
                     Database = new DatabaseConfig()
                     {
                         ConnectionString = TestHelper.ConnectionString,
                         AutoCreate = true
                     },
                     CommunicationWorkerOptions = options
                 };
                 if (orchestrationTypes != null)
                     sqlConfig.OrchestrationWorkerOptions.GetBuildInOrchestrators = (sp) => orchestrationTypes;
                 if (activityTypes != null)
                     sqlConfig.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) => activityTypes;
                 if (interfaceActivitys != null)
                     sqlConfig.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface = (sp) => interfaceActivitys;
                 services.UsingARMOrchestration<CustomCommunicationJob>(sp => sqlConfig);
                 services.UsingARMOrhcestrationClient<CustomCommunicationJob>(sp => sqlConfig);
                 services.AddSingleton<ICommunicationProcessor<CustomCommunicationJob>>((sp) =>
                 {
                     return new MockCommunicationProcessor();
                 });
                 services.AddSingleton<IInfrastructure>((sp) =>
                 {
                     if (infrastructure == null)
                         return new MockInfrastructure(sp);
                     return infrastructure;
                 });
             });
        }

        public static OrchestrationInstance OrchestrationTest(ARMOrchestartionFixture fixture,
            string filename,
            string subscriptionId,
            Func<OrchestrationInstance, OrchestrationCompletedArgs, bool> isValidateOrchestration = null,
             Action<OrchestrationInstance, OrchestrationCompletedArgs> validate = null,
             bool usingLinkTemplate = false)
        {
            var (instance, result) = OrchestrationTestNotCheckResult(fixture, filename, subscriptionId, isValidateOrchestration: isValidateOrchestration, validate: validate, usingLinkTemplate: usingLinkTemplate);
            Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
            var response = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
            Assert.Equal(200, response.Code);
            return instance;
        }

        public static (OrchestrationInstance, OrchestrationState) OrchestrationTestNotCheckResult(ARMOrchestartionFixture fixture,
            string filename,
            string subscriptionId,
            Func<OrchestrationInstance, OrchestrationCompletedArgs, bool> isValidateOrchestration = null,
            Action<OrchestrationInstance, OrchestrationCompletedArgs> validate = null, bool usingLinkTemplate = false
)
        {
            Deployment deployinput;
            if (usingLinkTemplate)
                deployinput = new Deployment()
                {
                    TemplateLink = new maskx.ARMOrchestration.ARMTemplate.TemplateLink() { Uri = filename },
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = subscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = TestHelper.TenantId
                };
            else
                deployinput = new Deployment()
                {
                    Template = TestHelper.GetTemplateContent(filename),
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = subscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = TestHelper.TenantId
                };
            return OrchestrationTestNotCheckResult(fixture, isValidateOrchestration, validate, deployinput);
        }

        private static (OrchestrationInstance, OrchestrationState) OrchestrationTestNotCheckResult(ARMOrchestartionFixture fixture, Func<OrchestrationInstance, OrchestrationCompletedArgs, bool> isValidateOrchestration, Action<OrchestrationInstance, OrchestrationCompletedArgs> validate, Deployment deployinput)
        {
            var deployment = fixture.ARMOrchestrationClient.Run(deployinput).Result;
            var instance = new OrchestrationInstance() { InstanceId = deployment.InstanceId, ExecutionId = deployment.ExecutionId };
            TaskCompletionSource<OrchestrationCompletedArgs> t = new TaskCompletionSource<OrchestrationCompletedArgs>();
            if (isValidateOrchestration != null)
            {
                fixture.OrchestrationWorker.RegistOrchestrationCompletedAction((args) =>
                {
                    if (isValidateOrchestration(instance, args))
                    {
                        t.SetResult(args);
                    }
                });
                var r = t.Task.Result;
                validate?.Invoke(instance, r);
            }
            var taskHubClient = new TaskHubClient(fixture.ServiceProvider.GetService<IOrchestrationServiceClient>());
            OrchestrationState result;
            while (true)
            {
                result = taskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    break;
                }
            }
            return (instance, result);
        }
    }
}