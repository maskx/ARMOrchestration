using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ARMOrchestrationTest.Mock
{
    public class MockInfrastructure : IInfrastructure
    {
        public AsyncRequestInput GetRequestInput(RequestOrchestrationInput input)
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
                RequestTo = input.RequestAction.ToString(),
                RequestOperation = "PUT",
                RequsetContent = input.Resource?.ToString(),
                RuleField = ruleField,
                Processor = "MockCommunicationProcessor"
            };
            return r;
        }

        public TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "")
        {
            return new TaskResult() { Content = value };
        }

        public TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false)
        {
            var pars = resourceName.TrimStart('/').Split('/');
            var c = TestHelper.GetJsonFileContent($"Mock/Response/{pars[pars.Length - 1]}");
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

        public TaskResult WhatIf(DeploymentContext context, string resourceName)
        {
            // var r = context.Template.Resources[resourceName];
            var c = TestHelper.GetJsonFileContent($"Mock/Response/{resourceName}");
            return new TaskResult() { Content = c, Code = 200 };
        }
    }
}