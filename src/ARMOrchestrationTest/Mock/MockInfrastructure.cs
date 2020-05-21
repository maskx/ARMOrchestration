using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ARMOrchestrationTest.Mock
{
    public class MockInfrastructure : IInfrastructure
    {
        private IServiceProvider serviceProvider;

        public MockInfrastructure(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TaskResult Providers(string providerNamespace, string resourceType)
        {
            return new TaskResult();
        }

        public AsyncRequestInput GetRequestInput(AsyncRequestActivityInput input)
        {
            Dictionary<string, object> ruleField = new Dictionary<string, object>();
            if (input.Resource != null)
            {
                ruleField.Add("ApiVersion", input.Resource.ApiVersion);
                ruleField.Add("Type", input.Resource.Type);
                ruleField.Add("Name", input.Resource.Name);
                ruleField.Add("Location", input.Resource.Location);
                ruleField.Add("SKU", input.Resource.SKU);
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
            var deploymentContext = input.DeploymentContext;
            ruleField.Add("SubscriptionId", deploymentContext.SubscriptionId);
            ruleField.Add("TenantId", deploymentContext.TenantId);
            ruleField.Add("ResourceGroup", deploymentContext.ResourceGroup);
            var r = new AsyncRequestInput()
            {
                EventName = input.ProvisioningStage.ToString(),
                RequestTo = input.Resource.FullType,
                RequestOperation = "PUT",
                RequestContent = input.Resource?.ToString(),
                RuleField = ruleField,
                Processor = "MockCommunicationProcessor"
            };
            return r;
        }

        public TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "")
        {
            var ret= TestHelper.GetJsonFileContent($"Mock/Response/list");
            return new TaskResult() { Content = ret };
        }

        public TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false)
        {
            string c = string.Empty;
            var pars = resourceName.TrimStart('/').Split('/');
            if (pars[pars.Length - 2].Equals("deployments", StringComparison.OrdinalIgnoreCase)
                && pars[pars.Length - 3].Equals("Microsoft.Resources", StringComparison.OrdinalIgnoreCase))
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
                c = TestHelper.GetJsonFileContent($"Mock/Response/{pars[pars.Length - 1]}");
            if (!full)
                c = JObject.Parse(c)["properties"].ToString(Newtonsoft.Json.Formatting.None);
            return new TaskResult()
            {
                Code = 200,
                Content = c
            };
        }

        public BuiltinServiceTypes BuitinServiceTypes { get; set; } = new BuiltinServiceTypes();
        public List<string> ExtensionResources { get; set; } = new List<string>() { "tags" };
        public BuiltinPathSegment BuiltinPathSegment { get; set; } = new BuiltinPathSegment();
        public List<(string Name, string Version)> BeforeDeploymentOrchestration { get; set; }
        public List<(string Name, string Version)> AfterDeploymentOrhcestration { get; set; }
        public List<(string Name, string Version)> BeforeResourceProvisioningOrchestation { get; set; }
        public List<(string Name, string Version)> AfterResourceProvisioningOrchestation { get; set; }
        public bool InjectBeforeDeployment { get; set; } = false;
        public bool InjectAfterDeployment { get; set; } = false;
        public bool InjectBefroeProvisioning { get; set; } = false;
        public bool InjectAfterProvisioning { get; set; } = false;

        public TaskResult WhatIf(DeploymentContext context, string resourceName)
        {
            // var r = context.Template.Resources[resourceName];
            var c = TestHelper.GetJsonFileContent($"Mock/Response/{resourceName}");
            return new TaskResult() { Content = c, Code = 200 };
        }
    }
}