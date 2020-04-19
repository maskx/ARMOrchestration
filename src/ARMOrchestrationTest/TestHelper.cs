using ARMOrchestrationTest.Mock;
using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.SQL;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ARMCreatorTest
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

        public static IOptions<ARMOrchestrationOptions> ARMOrchestrationOptions { get; private set; }

        public static IConfigurationRoot Configuration { get; private set; }
        public static DataConverter DataConverter { get; private set; } = new JsonDataConverter();
        public static string SubscriptionId = "C1FA36C2-4D58-45E8-9C51-498FADB4D8BF";
        public static string ResourceGroup = "ResourceGroup1";

        public static string ConnectionString
        {
            get
            {
                return Configuration.GetConnectionString("dbConnection");
            }
        }

        public static TaskHubClient TaskHubClient { get; private set; }

        static TestHelper()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("afab7740-fb18-44a0-9f16-b94c3327da7e")
                .Build();
            TaskHubClient = new TaskHubClient(CreateOrchestrationClient());
            ARMOrchestrationOptions = Options.Create(new ARMOrchestrationOptions());
        }

        public static IOrchestrationServiceClient CreateOrchestrationClient()
        {
            return new SQLServerOrchestrationService(
                         TestHelper.ConnectionString,
                         TestHelper.HubName,
                         CreateSQLServerInstanceStore(),
                         CreateOrchestrationServiceSettings());
        }

        public static SqlServerInstanceStore CreateSQLServerInstanceStore()
        {
            return new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
            {
                HubName = TestHelper.HubName,
                ConnectionString = TestHelper.ConnectionString
            });
        }

        public static string HubName { get { return Configuration["HubName"]; } }

        public static SQLServerOrchestrationServiceSettings CreateOrchestrationServiceSettings(CompressionStyle style = CompressionStyle.Threshold)
        {
            var settings = new SQLServerOrchestrationServiceSettings
            {
                TaskOrchestrationDispatcherSettings = { CompressOrchestrationState = true },
            };
            settings.TaskOrchestrationDispatcherSettings.DispatcherCount = 4;
            settings.TaskActivityDispatcherSettings.DispatcherCount = 4;
            return settings;
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

        public static OrchestrationInstance FunctionTest(OrchestrationWorker worker, string filename, Dictionary<string, string> result)
        {
            var templateString = TestHelper.GetFunctionInputContent(filename);
            var instance = worker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = "DeploymentOrchestration",
                    Version = "1.0"
                },
                Input = TestHelper.DataConverter.Serialize(new DeploymentOrchestrationInput()
                {
                    TemplateContent = templateString,
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    DeploymentName = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005"
                })
            }).Result;
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();

            worker.RegistOrchestrationCompletedAction((args) =>
            {
                if (!args.IsSubOrchestration && args.InstanceId == instance.InstanceId)
                    t.SetResult(args.Result);
            });
            t.Task.Wait();
            var outputString = DataConverter.Deserialize<TaskResult>(t.Task.Result).Content;

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

        public static IOrchestrationService CreateOrchestrationService()
        {
            var service = new SQLServerOrchestrationService(
                         TestHelper.ConnectionString,
                         TestHelper.HubName,
                         CreateSQLServerInstanceStore(),
                         CreateOrchestrationServiceSettings());
            service.CreateIfNotExistsAsync().Wait();
            return service;
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
                 .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
             })
             .ConfigureServices((hostContext, services) =>
             {
                 config?.Invoke(hostContext, services);
                 services.AddHttpClient();
                 CommunicationWorkerOptions options = new CommunicationWorkerOptions();
                 options.AutoCreate = true;
                 if (communicationWorkerOptions != null)
                 {
                     options.GetFetchRules = communicationWorkerOptions.GetFetchRules;
                     options.HubName = communicationWorkerOptions.HubName;
                     options.MaxConcurrencyRequest = communicationWorkerOptions.MaxConcurrencyRequest;
                     options.RuleFields.AddRange(communicationWorkerOptions.RuleFields);
                     options.SchemaName = communicationWorkerOptions.SchemaName;
                 }
                 options.RuleFields.Add("ApiVersion");
                 options.RuleFields.Add("Type");
                 options.RuleFields.Add("Name");
                 options.RuleFields.Add("Location");
                 options.RuleFields.Add("SKU");
                 options.RuleFields.Add("Kind");
                 options.RuleFields.Add("Plan");
                 options.RuleFields.Add("SubscriptionId");
                 options.RuleFields.Add("TenantId");
                 options.RuleFields.Add("ResourceGroup");
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
                 services.UsingARMOrchestration(sqlConfig);
                 services.AddSingleton<IInfrastructure>((sp) => new MockInfrastructure(sp));
                 services.AddSingleton<ICommunicationProcessor>((sp) =>
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

        public static OrchestrationInstance OrchestrationTest(OrchestrationWorker worker,
            string filename,
            Func<OrchestrationInstance, OrchestrationCompletedArgs, bool> isValidateOrchestration = null,
            Action<OrchestrationInstance, OrchestrationCompletedArgs> validate = null)
        {
            var instance = worker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = "DeploymentOrchestration",
                    Version = "1.0"
                },
                Input = TestHelper.DataConverter.Serialize(new DeploymentOrchestrationInput()
                {
                    TemplateContent = TestHelper.GetTemplateContent(filename),
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    DeploymentName = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005"
                })
            }).Result;
            TaskCompletionSource<OrchestrationCompletedArgs> t = new TaskCompletionSource<OrchestrationCompletedArgs>();
            if (isValidateOrchestration != null)
            {
                worker.RegistOrchestrationCompletedAction((args) =>
                {
                    if (isValidateOrchestration(instance, args))
                    {
                        t.SetResult(args);
                    }
                });
                var r = t.Task.Result;
                validate?.Invoke(instance, r);
            }
            while (true)
            {
                var result = TestHelper.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    break;
                }
            }
            return instance;
        }

        public static async Task<List<DeploymentOperation>> GetDeploymentOpetions(string deploymentId)
        {
            List<DeploymentOperation> r = new List<DeploymentOperation>();
            using (var db = new DbAccess(TestHelper.ConnectionString))
            {
                db.AddStatement($"select * from arm_DeploymentOperations where deploymentId=N'{deploymentId}'");
                await db.ExecuteReaderAsync((reader, index) =>
                  {
                      r.Add(new DeploymentOperation()
                      {
                          Name = reader["Resource"].ToString(),
                          ResourceId = reader["ResourceId"].ToString(),
                          Result = reader["Result"]?.ToString()
                      });
                  });
            }
            return r;
        }
    }
}