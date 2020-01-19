using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Workers;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.SQL;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ARMCreatorTest
{
    public class TestHelper
    {
        public static AsyncRequestInput CreateAsyncRequestInput(string processorName, ResourceOrchestrationInput input)
        {
            var resource = new Resource(input.Resource, input.OrchestrationContext);
            Dictionary<string, object> ruleField = new Dictionary<string, object>();
            ruleField.Add("ApiVersion", ARMFunctions.Evaluate(resource.ApiVersion, input.OrchestrationContext));
            ruleField.Add("Type", ARMFunctions.Evaluate(resource.Type, input.OrchestrationContext));
            ruleField.Add("Name", ARMFunctions.Evaluate(resource.Name, input.OrchestrationContext));
            ruleField.Add("Location", ARMFunctions.Evaluate(resource.Location, input.OrchestrationContext));
            ruleField.Add("SKU", resource.SKU);
            ruleField.Add("Kind", ARMFunctions.Evaluate(resource.Kind, input.OrchestrationContext));
            ruleField.Add("Plan", resource.Plan);
            return new AsyncRequestInput()
            {
                RequestTo = "ResourceProvider",// TODO: 支持Subscription level Resource和Tenant level Resource后，将有不同的ResourceTo
                RequestOperation = "PUT",//ResourceProvider 处理 Create Or Update
                RequsetContent = resource.Properties,
                //   RuleField = ruleField,
                Processor = processorName
            };
        }

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
                TaskOrchestrationDispatcherSettings = { CompressOrchestrationState = true }
            };

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
            return File.ReadAllText(s);
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

        public static void FunctionTest(OrchestrationWorker worker, string filename, Dictionary<string, string> result)
        {
            var templateString = TestHelper.GetFunctionInputContent(filename);
            var instance = worker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(TemplateOrchestration).FullName + "_"
                },
                Input = TestHelper.DataConverter.Serialize(new TemplateOrchestrationInput()
                {
                    Template = templateString,
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup
                })
            }).Result;
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();

            worker.RegistOrchestrationCompletedAction((args) =>
            {
                if (!args.IsSubOrchestration && args.InstanceId == instance.InstanceId)
                    t.SetResult(args.Result);
            });
            var outputString = DataConverter.Deserialize<TaskResult>(t.Task.Result).Content;

            using var templateDoc = JsonDocument.Parse(templateString);
            using var outputDoc = JsonDocument.Parse(outputString);
            var outputRoot = outputDoc.RootElement;
            if (templateDoc.RootElement.TryGetProperty("outputs", out JsonElement outputDefineElement))
            {
                List<string> child = new List<string>();
                foreach (var item in outputDefineElement.EnumerateObject())
                {
                    Assert.True(outputRoot.TryGetProperty(item.Name, out JsonElement v), $"cannot find {item.Name} in output");
                    if (v.ValueKind == JsonValueKind.String)
                        Assert.True(result[item.Name] == v.GetString(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetString()}");
                    else
                        Assert.True(result[item.Name] == v.GetRawText(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetRawText()}");
                }
            }
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
            List<Type> orchestrationTypes = null,
            List<Type> activityTypes = null,
            IDictionary<Type, object> interfaceActivitys = null,
            Action<HostBuilderContext, IServiceCollection> config = null)
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
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationClient();
                 });
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationService();
                 });
                 services.Configure<ARMOrchestrationOptions>((opt) =>
                 {
                     opt.ExtensionResources = new Dictionary<string, Func<ResourceOrchestrationInput, string, string, AsyncRequestInput>>() {
                         {"tags",(input,name,property)=>{return new AsyncRequestInput(){
                             Processor = "MockCommunicationProcessor",
                             RequestOperation = "Create",
                             RequestTo = name,
                             RequsetContent=property
                         }; } }
                     };
                 });

                 #region OrchestrationWorker

                 services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
                 {
                     OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                     orchestrationCreatorFactory.RegistCreator("DICreator", typeof(DICreator<TaskOrchestration>));
                     orchestrationCreatorFactory.RegistCreator("DefaultObjectCreator", typeof(DefaultObjectCreator<TaskOrchestration>));
                     return orchestrationCreatorFactory;
                 });
                 if (orchestrationTypes == null)
                     orchestrationTypes = new List<Type>();
                 if (activityTypes == null)
                     activityTypes = new List<Type>();

                 orchestrationTypes.Add(typeof(AsyncRequestOrchestration));
                 orchestrationTypes.Add(typeof(ResourceOrchestration));
                 orchestrationTypes.Add(typeof(TemplateOrchestration));
                 orchestrationTypes.Add(typeof(WaitDependsOnOrchestration));
                 orchestrationTypes.Add(typeof(CopyOrchestration));
                 activityTypes.Add(typeof(AsyncRequestActivity));
                 activityTypes.Add(typeof(HttpRequestActivity));
                 activityTypes.Add(typeof(DeploymentOperationsActivity));
                 activityTypes.Add(typeof(WaitDependsOnActivity));
                 activityTypes.Add(typeof(PrepareTemplateActivity));
                 activityTypes.Add(typeof(PrepareResourceActivity));
                 services.Configure<OrchestrationWorkerOptions>(options =>
                 {
                     options.GetBuildInOrchestrators = (sp) => orchestrationTypes;
                     options.GetBuildInTaskActivities = (sp) => activityTypes;
                     if (interfaceActivitys != null)
                         options.GetBuildInTaskActivitiesFromInterface = (sp) => interfaceActivitys;
                 });

                 services.AddSingleton<OrchestrationWorker>();
                 services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());

                 #endregion OrchestrationWorker

                 #region CommunicationWorker

                 services.Configure<CommunicationWorkerOptions>((options) =>
               {
                   TestHelper.Configuration.GetSection("CommunicationWorker").Bind(options);
                   options.AutoCreate = true;
                   if (communicationWorkerOptions != null)
                   {
                       options.GetFetchRules = communicationWorkerOptions.GetFetchRules;
                       options.HubName = communicationWorkerOptions.HubName;
                       options.MaxConcurrencyRequest = communicationWorkerOptions.MaxConcurrencyRequest;
                       options.RuleFields.AddRange(communicationWorkerOptions.RuleFields);
                       options.SchemaName = communicationWorkerOptions.SchemaName;
                   }
               });
                 services.AddHostedService<CommunicationWorker>();

                 #endregion CommunicationWorker

                 #region WaitDependsOnWorker

                 services.AddSingleton<WaitDependsOnWorker>();
                 services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker>());

                 #endregion WaitDependsOnWorker
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
                    Creator = "DICreator",
                    Uri = typeof(TemplateOrchestration).FullName + "_"
                },
                Input = TestHelper.DataConverter.Serialize(new TemplateOrchestrationInput()
                {
                    Template = TestHelper.GetTemplateContent(filename),
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup
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

        public static async Task<List<DeploymentOperationsActivityInput>> GetDeploymentOpetions(string deploymentId)
        {
            List<DeploymentOperationsActivityInput> r = new List<DeploymentOperationsActivityInput>();
            using (var db = new DbAccess(TestHelper.ConnectionString))
            {
                db.AddStatement($"select * from arm_DeploymentOperations where deploymentId=N'{deploymentId}'");
                await db.ExecuteReaderAsync((reader, index) =>
                  {
                      r.Add(new DeploymentOperationsActivityInput()
                      {
                          Resource = reader["Resource"].ToString(),
                          ResourceId = reader["ResourceId"].ToString(),
                          Result = reader["Result"]?.ToString()
                      });
                  });
            }
            return r;
        }
    }
}