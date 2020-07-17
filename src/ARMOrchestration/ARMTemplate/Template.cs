using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Template
    {
        public string Schema { get; set; }

        public string ContentVersion { get; set; }

        public string ApiProfile { get; set; }

        public string Parameters { get; set; }

        public string Variables { get; set; }

        public ResourceCollection Resources { get; set; } = new ResourceCollection();

        public Functions Functions { get; set; }

        public string Outputs { get; set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-subscription
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-management-group
        /// </summary>
        public DeployLevel DeployLevel { get; set; }

        internal List<string> ConditionFalseResources { get; private set; } = new List<string>();
        public static Template Parse(JsonElement root, DeploymentContext input, ARMFunctions functions, IInfrastructure infrastructure)
        {
            Template template = new Template();
            input.Template = template;
            Dictionary<string, object> context = new Dictionary<string, object>() {
                {ContextKeys.ARM_CONTEXT, input},
                {ContextKeys.IS_PREPARE,true }
            };
            if (!root.TryGetProperty("$schema", out JsonElement schema))
                throw new Exception("not find $schema in template");
            template.Schema = schema.GetString();

            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                throw new Exception("not find contentVersion in template");
            template.ContentVersion = contentVersion.GetString();

            if (template.Schema.EndsWith("deploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ResourceGroup;
            else if (template.Schema.EndsWith("subscriptionDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.Subscription;
            else if (template.Schema.EndsWith("managementGroupDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ManagemnetGroup;
            else
                throw new Exception("wrong $shema setting");

            if (!root.TryGetProperty("resources", out JsonElement resources))
                throw new Exception("not find resources in template");


            if (root.TryGetProperty("apiProfile", out JsonElement apiProfile))
                template.ApiProfile = apiProfile.GetString();

            if (root.TryGetProperty("parameters", out JsonElement parameters))
                template.Parameters = parameters.GetRawText();

            if (root.TryGetProperty("outputs", out JsonElement outputs))
                template.Outputs = outputs.GetRawText();
            if (root.TryGetProperty("variables", out JsonElement variables))
            {
                // cos var can reference var
                template.Variables = variables.GetRawText();
                template.Variables = variables.ExpandObject(context, functions, infrastructure);
            }
            if (root.TryGetProperty("functions", out JsonElement funcs))
            {
                template.Functions = Functions.Parse(funcs);
            }
            foreach (var resource in resources.EnumerateArray())
            {
                foreach (var r in Resource.Parse(resource, context, functions, infrastructure, string.Empty, string.Empty))
                {
                    if (r.Condition)
                        template.Resources.Add(r);
                    else
                        template.ConditionFalseResources.Add(r.Name);
                }
            }
            return template;
        }
        public static Template Parse(string content, DeploymentContext input, ARMFunctions functions, IInfrastructure infrastructure)
        {
            using JsonDocument doc = JsonDocument.Parse(content);
            return Parse(doc.RootElement, input, functions, infrastructure);
        }

        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();

            writer.WriteString("$schema", this.Schema);
            writer.WriteString("contentVersion", this.ContentVersion);
            if (!string.IsNullOrEmpty(this.ApiProfile))
                writer.WriteString("apiProfile", this.ApiProfile);
            if (!string.IsNullOrEmpty(this.Parameters))
                writer.WriteRawString("parameters", this.Parameters);
            if (!string.IsNullOrEmpty(this.Variables))
                writer.WriteRawString("variables", this.Variables);
            if (!string.IsNullOrEmpty(this.Outputs))
                writer.WriteRawString("outputs", this.Outputs);
            writer.WritePropertyName("resources");
            writer.WriteStartArray();
            foreach (var r in Resources)
            {
                writer.WriteRawString(r.ToString());
            }
            writer.WriteEndArray();
            if(this.Functions!=null)
            {
                writer.WriteRawString("functions",this.Functions.ToString());
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}