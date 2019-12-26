using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Serializing;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationCreator;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.OrchestrationCreator;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace ARMCreatorTest
{
    public class TestHelper
    {
        public static IConfigurationRoot Configuration { get; private set; }
        public static DataConverter DataConverter { get; private set; } = new JsonDataConverter();

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
            string s = Path.Combine(AppContext.BaseDirectory, "Functions", $"{filename}.json");
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

        public static void FunctionTest(string filename, Dictionary<string, string> result)
        {
            var templateString = TestHelper.GetFunctionInputContent(filename);
            ARMOrchestration orchestration = new ARMOrchestration();
            var outputString = orchestration.RunTask(null, TestHelper.DataConverter.Serialize(new ARMOrchestrationInput()
            {
                Template = Template.Parse(templateString)
            })).Result.Content;
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
                 if (config != null)
                     config(hostContext, services);
                 services.AddHttpClient();
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationClient();
                 });
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationService();
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
                 List<Type> activityTypes = new List<Type>();

                 orchestrationTypes.Add(typeof(AsyncRequestOrchestration));
                 orchestrationTypes.Add(typeof(ResourceOrchestration));
                 orchestrationTypes.Add(typeof(ARMOrchestration));

                 activityTypes.Add(typeof(AsyncRequestActivity));
                 activityTypes.Add(typeof(HttpRequestActivity));
                 services.Configure<OrchestrationWorkerOptions>(options =>
                 {
                     options.GetBuildInOrchestrators = () => orchestrationTypes;
                     options.GetBuildInTaskActivities = () => activityTypes;
                 });

                 services.AddSingleton<OrchestrationWorker>();
                 services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());

                 #endregion OrchestrationWorker

                 #region CommunicationWorker

                 services.Configure<CommunicationWorkerOptions>((options) =>
               {
                   TestHelper.Configuration.GetSection("CommunicationWorker").Bind(options);
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
             });
        }

        public static void OrchestrationTest(OrchestrationWorker worker, string filename)
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };

            worker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new Orchestration()
                {
                    Creator = "DICreator",
                    Uri = typeof(ARMOrchestration).FullName + "_"
                },
                Input = TestHelper.DataConverter.Serialize(new ARMOrchestrationInput()
                {
                    Template = Template.Parse(TestHelper.GetTemplateContent(filename)),
                    Parameters = string.Empty
                })
            }).Wait();
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
        }
    }
}