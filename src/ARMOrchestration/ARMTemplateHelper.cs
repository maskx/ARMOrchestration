using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        public readonly ARMFunctions functions;
        private readonly IServiceProvider serviceProvider;

        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions functions,
            IServiceProvider service)
        {
            this.options = options?.Value;
            this.functions = functions;
            this.serviceProvider = service;
        }

        public (bool Result, string Message, Template Template) ValidateTemplate(DeploymentOrchestrationInput input)
        {
            Template template = new Template();
            DeploymentContext deploymentContext = new DeploymentContext()
            {
                CorrelationId = input.CorrelationId,
                DeploymentId = input.DeploymentId,
                Mode = input.Mode,
                ResourceGroup = input.ResourceGroup,
                SubscriptionId = input.SubscriptionId,
                TenantId = input.TenantId,
                Parameters = input.Parameters,
                Template = template
            };
            using JsonDocument doc = JsonDocument.Parse(input.Template);
            var root = doc.RootElement;
            if (!root.TryGetProperty("$schema", out JsonElement schema))
                return (false, "not find $schema in template", null);
            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                return (false, "not find contentVersion in template", null);
            if (!root.TryGetProperty("resources", out JsonElement resources))
                return (false, "not find resources in template", null);
            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                {"armcontext", deploymentContext} };

            template.Schema = schema.GetString();
            template.ContentVersion = contentVersion.GetString();

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
                template.Variables = variables.ExpandObject(armContext, this);
            }

            if (root.TryGetProperty("functions", out JsonElement functions))
            {
                var fr = Functions.Parse(functions.GetRawText());
                if (fr.Result)
                    template.Functions = fr.Functions;
                else
                    return (false, fr.Message, null);
            }
            foreach (var resource in resources.EnumerateArray())
            {
                if (resource.TryGetProperty("copy", out JsonElement copy))
                {
                    var c = ParseCopy(copy.GetRawText(), armContext);
                    var copyResult = resource.ExpandCopyResource(c.Copy, armContext, options, this);
                    if (copyResult.Result)
                    {
                        c.Copy.Resources = copyResult.Resources;
                        template.Copys.Add(c.Copy.Name, c.Copy);
                    }
                    else
                        return (false, copyResult.Message, null);
                }
                else
                {
                    var resResult = ParseResource(resource.GetRawText(), armContext);
                    if (resResult.Result)
                        template.Resources.Add(resResult.resource.ResouceId, resResult.resource);
                    else
                        return (false, resResult.Message, null);
                }
            }
            return (true, string.Empty, template);
        }

        public WhatIfOperationResult WhatIf(PredictTemplateOrchestrationInput input)
        {
            var result = new WhatIfOperationResult();
            var valid = ValidateTemplate(new DeploymentOrchestrationInput()
            {
                CorrelationId = input.CorrelationId,
                Parameters = input.Parameters,
                ResourceGroup = input.ResourceGroupName,
                SubscriptionId = input.SubscriptionId,
                Template = input.Template,
                TenantId = input.TenantId
            });
            if (!valid.Result)
            {
                result.Status = "failed";
                result.Error = new ErrorResponse() { Code = "400", Message = valid.Message };
            }
            string queryScope;
            if (input.ScopeType == ScopeType.ResourceGroup)
                queryScope = $"subscriptions/{input.SubscriptionId}/resourceGroups/{input.ResourceGroupName}/resources";
            else
                queryScope = $"subscriptions/{input.SubscriptionId}/resources";
            var str = this.options.ListFunction(serviceProvider, queryScope, valid.Template.ApiProfile);
            using var doc = JsonDocument.Parse(str.Content);
            foreach (var r in doc.RootElement.EnumerateArray())
            {
                if (!r.TryGetProperty("id", out JsonElement id))
                    break;
                id.GetString();
            }
            result.Status = "succeeded";
            return result;
        }

        public (bool Result, string Message, Copy Copy) ParseCopy(string jsonString, Dictionary<string, object> context)
        {
            var copy = new Copy();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            if (root.TryGetProperty("name", out JsonElement name))
                copy.Name = name.GetString();
            else
                return (false, "not find name in copy node", null);

            if (root.TryGetProperty("count", out JsonElement count))
            {
                if (count.ValueKind == JsonValueKind.Number)
                    copy.Count = count.GetInt32();
                else if (count.ValueKind == JsonValueKind.String)
                    copy.Count = (int)functions.Evaluate(count.GetString(), context);
                else
                    return (false, "the value of count property should be Number in copy node", null);
            }
            else
                return (false, "not find count in copy node", null);
            if (root.TryGetProperty("mode", out JsonElement mode))
            {
                copy.Mode = mode.GetString().ToLower();
            }
            if (root.TryGetProperty("batchSize", out JsonElement batchSize))
            {
                if (batchSize.ValueKind == JsonValueKind.Number)
                    copy.BatchSize = batchSize.GetInt32();
                else if (batchSize.ValueKind == JsonValueKind.String)
                    copy.BatchSize = (int)functions.Evaluate(batchSize.GetString(), context);
            }
            if (root.TryGetProperty("input", out JsonElement input))
            {
                copy.Input = input.GetRawText();
            }
            copy.Id = $"deployment/{(context["armcontext"] as DeploymentContext).DeploymentId}/copy/{copy.Name}";
            return (true, string.Empty, copy);
        }

        public (bool Result, string Message, Resource resource)
                ParseResource(string jsonString,
            Dictionary<string, object> context,
            string parentName = "",
            string parentType = "")
        {
            DeploymentContext deploymentContext = context["armcontext"] as DeploymentContext;
            Resource r = new Resource();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.False)
                    r.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    r.Condition = (bool)functions.Evaluate(condition.GetString(), context);
            }
            if (root.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                return (false, "not find apiVersion in resource node", null);
            if (root.TryGetProperty("type", out JsonElement type))
            {
                r.Type = functions.Evaluate(type.GetString(), context).ToString();
                if (!string.IsNullOrEmpty(parentType))
                    r.Type = $"{parentType}/{r.Type}";
            }
            else
                return (false, "not find type in resource node", null);
            if (root.TryGetProperty("name", out JsonElement name))
            {
                r.Name = functions.Evaluate(name.GetString(), context).ToString();
                if (!string.IsNullOrEmpty(parentName))
                    r.Name = $"{parentName}/{r.Name}";
            }
            else
                return (false, "not find name in resource node", null);
            if (root.TryGetProperty("location", out JsonElement location))
                r.Location = functions.Evaluate(location.GetString(), context).ToString();
            if (root.TryGetProperty("tags", out JsonElement tags))
                r.Tags = tags.GetRawText();
            if (root.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();
            if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                r.DependsOn = new List<string>();
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(functions.Evaluate(item.GetString(), context).ToString());
                }
            }
            if (root.TryGetProperty("properties", out JsonElement properties))
                r.Properties = properties.ExpandObject(context, this);
            if (root.TryGetProperty("sku", out JsonElement sku))
                r.SKU = sku.GetString();
            if (root.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetString();
            if (root.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetString();
            if (root.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                r.ResourceGroup = functions.Evaluate(resourceGroup.GetString(), context).ToString();
            else
                r.ResourceGroup = deploymentContext.ResourceGroup;
            if (root.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                r.SubscriptionId = functions.Evaluate(subscriptionId.GetString(), context).ToString();
            else
                r.SubscriptionId = deploymentContext.SubscriptionId;

            #region ResouceId

            var t = deploymentContext.Template;
            if (t.DeployLevel == Template.ResourceGroupDeploymentLevel)
                r.ResouceId = functions.resourceId(
                    deploymentContext,
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.Type,
                    r.Name);
            else if (t.DeployLevel == Template.SubscriptionDeploymentLevel)
                r.ResouceId = functions.subscriptionResourceId(deploymentContext, r.SubscriptionId, r.Type, r.Name);
            else
                r.ResouceId = functions.tenantResourceId(r.Type, r.Name);

            #endregion ResouceId

            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                foreach (var childres in resources.EnumerateArray())
                {
                    //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
                    var childResult = ParseResource(childres.GetRawText(), context, r.Name, r.Type);
                    if (childResult.Result)
                    {
                        r.Resources.Add(childResult.resource);
                    }
                    else
                        return (false, childResult.Message, null);
                }
            }

            foreach (var item in options.ExtensionResources)
            {
                if (root.TryGetProperty(item, out JsonElement e))
                {
                    r.ExtensionResource.Add(item, e.GetRawText());
                }
            }
            return (true, string.Empty, r);
        }
    }
}