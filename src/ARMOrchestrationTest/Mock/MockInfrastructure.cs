using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.Mock
{
    public class MockInfrastructure : IInfrastructure
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpClientFactory _HttpClientFactory;
        static IMemoryCache _TemplateCache = new MemoryCache(Options.Create(new MemoryCacheOptions() { }));
        public MockInfrastructure(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this._HttpClientFactory = serviceProvider?.GetService<IHttpClientFactory>();
        }

        public (string GroupId, string GroupType, string HierarchyId) GetGroupInfo(string managementGroupId, string subscriptionId, string resourceGroupName)
        {
            return ("3128C563-AC63-488E-8507-C47B3B9C0DBD", "ResourceGroup", "001004005008");
        }

        public TaskResult Providers(string providerNamespace, string resourceType)
        {
            return new TaskResult();
        }

        public AsyncRequestInput GetRequestInput(AsyncRequestActivityInput input)
        {
            Dictionary<string, object> ruleField = new Dictionary<string, object>();
            input.ServiceProvider = serviceProvider;
            if (input.Resource != null)
            {
                ruleField.Add("ApiVersion", input.Resource.ApiVersion);
                ruleField.Add("Type", input.Resource.Type);
                ruleField.Add("Name", input.Resource.Name);
                ruleField.Add("Location", input.Resource.Location);
                ruleField.Add("SKU", input.Resource.SKU?.Name);
                ruleField.Add("Kind", input.Resource.Kind);
                ruleField.Add("Plan", input.Resource.Plan);
            }
            else
            {
                ruleField.Add("ApiVersion", DBNull.Value);
                ruleField.Add("Type", DBNull.Value);
                ruleField.Add("Name", DBNull.Value);
                ruleField.Add("Location", DBNull.Value);
                ruleField.Add("SKU", DBNull.Value);
                ruleField.Add("Kind", DBNull.Value);
                ruleField.Add("Plan", DBNull.Value);
            }
            var deploymentContext = input.Input;
            ruleField.Add("SubscriptionId", deploymentContext.SubscriptionId);
            ruleField.Add("TenantId", deploymentContext.TenantId);
            ruleField.Add("ResourceGroup", deploymentContext.ResourceGroup);
            var r = new AsyncRequestInput()
            {
                EventName = input.ProvisioningStage.ToString(),
                RequestTo = input.Resource.Type,
                RequestOperation = "PUT",
                RequestContent = input.Resource?.ToString(),
                RuleField = ruleField,
                Processor = "MockCommunicationProcessor"
            };
            return r;
        }

        public TaskResult List(maskx.ARMOrchestration.Deployment context, string resourceId, string apiVersion, string functionValues = "", string value = "")
        {
            var ret = TestHelper.GetJsonFileContent($"Mock/Response/list");
            return new TaskResult() { Content = ret };
        }

        public TaskResult Reference(maskx.ARMOrchestration.Deployment context, string resourceName, string apiVersion = "", bool full = false)
        {
            string c = string.Empty;
            var pars = resourceName.TrimStart('/').Split('/');
            if (pars[^2].Equals("deployments", StringComparison.OrdinalIgnoreCase)
                && pars[^3].Equals("Microsoft.Resources", StringComparison.OrdinalIgnoreCase))
            {
                var client = this.serviceProvider.GetService<ARMOrchestrationClient>();
                var rs = client.GetAllResourceListAsync(context.RootId).Result;
                foreach (var item in rs)
                {
                    if (item.ResourceId.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        c = item.Result;
                        break;
                    }
                }
            }
            else
                c = TestHelper.GetJsonFileContent($"Mock/Response/{pars[^1]}");
            if (!full)
                c = JObject.Parse(c)["properties"].ToString(Newtonsoft.Json.Formatting.None);
            return new TaskResult()
            {
                Code = 200,
                Content = c
            };
        }

        public BuiltinServiceTypes BuiltinServiceTypes { get; set; } = new BuiltinServiceTypes();
        public List<string> ExtensionResources { get; set; } = new List<string>() { "tags" };
        public BuiltinPathSegment BuiltinPathSegment { get; set; } = new BuiltinPathSegment();
        public List<(string Name, string Version)> BeforeDeploymentOrchestration { get; set; } = new List<(string Name, string Version)>();
        public List<(string Name, string Version)> AfterDeploymentOrhcestration { get; set; } = new List<(string Name, string Version)>();
        public List<(string Name, string Version)> BeforeResourceProvisioningOrchestation { get; set; } = new List<(string Name, string Version)>();
        public List<(string Name, string Version)> AfterResourceProvisioningOrchestation { get; set; } = new List<(string Name, string Version)>();
        public bool InjectBeforeDeployment { get; set; } = false;
        public bool InjectAfterDeployment { get; set; } = false;
        public bool InjectBefroeProvisioning { get; set; } = false;
        public bool InjectAfterProvisioning { get; set; } = false;

        public TaskResult WhatIf(Deployment context, string resourceName)
        {
            // var r = context.Template.Resources[resourceName];
            var c = TestHelper.GetJsonFileContent($"Mock/Response/{resourceName}");
            return new TaskResult() { Content = c, Code = 200 };
        }
        public async Task<string> GetTemplateContentAsync(TemplateLink link, Deployment input)
        {
            if (!_TemplateCache.TryGetValue(link, out object t))
            {
                if (link.Uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                || link.Uri.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                {
                    var client = _HttpClientFactory.CreateClient();
                    t = await client.GetStringAsync(link.Uri);
                }
                else
                {
                    t = TestHelper.GetJsonFileContent(link.Uri);
                }
                if (t != null)
                    _TemplateCache.Set(link, t, DateTimeOffset.Now.AddMinutes(5));
            }
            return t?.ToString();
        }
        public async Task<string> GetParameterContentAsync(ParametersLink link, Deployment input)
        {
            if (link.Uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
               || link.Uri.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                var client = _HttpClientFactory.CreateClient();
                return await client.GetStringAsync(link.Uri);
            }
            else
            {
                return TestHelper.GetJsonFileContent(link.Uri);
            }
        }

    }
}