using DurableTask.Core.Serializing;
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
        private readonly IInfrastructure infrastructure;

        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions functions,
            IServiceProvider service,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.functions = functions;
            this.serviceProvider = service;
            this.infrastructure = infrastructure;
        }

        public (bool Result, string Message, Template Template) ValidateTemplate(DeploymentOrchestrationInput input)
        {
            Template template = new Template()
            {
                innerString = input.Template
            };
            DeploymentContext deploymentContext = new DeploymentContext()
            {
                CorrelationId = input.CorrelationId,
                RootId = input.DeploymentId,
                Mode = input.Mode,
                ResourceGroup = input.ResourceGroup,
                SubscriptionId = input.SubscriptionId,
                TenantId = input.TenantId,
                Parameters = input.Parameters,
                Template = template,
                TemplateLink = input.TemplateLink,
                ParametersLink = input.ParametersLink
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
            if (template.Schema.EndsWith("deploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ResourceGroup;
            else if (template.Schema.EndsWith("subscriptionDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.Subscription;
            else if (template.Schema.EndsWith("managementGroupDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ManagemnetGroup;
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
            DeploymentContext deploymentContext = new DeploymentContext()
            {
                CorrelationId = input.CorrelationId,
                Mode = input.Mode,
                ResourceGroup = input.ResourceGroupName,
                SubscriptionId = input.SubscriptionId,
                TenantId = input.TenantId,
                Parameters = input.Parameters,
                Template = valid.Template,
                TemplateLink = input.TemplateLink,
                ParametersLink = input.ParametersLink
            };
            string queryScope;
            if (input.ScopeType == ScopeType.ResourceGroup)
                queryScope = $"subscriptions/{input.SubscriptionId}/resourceGroups/{input.ResourceGroupName}";
            else
                queryScope = $"subscriptions/{input.SubscriptionId}";
            var str = this.infrastructure.List(deploymentContext, queryScope, valid.Template.ApiProfile, string.Empty, "resources");
            //https://docs.microsoft.com/en-us/rest/api/resources/resources/listbyresourcegroup#resourcelistresult
            using var doc = JsonDocument.Parse(str.Content);
            Dictionary<string, JsonElement> asset = new Dictionary<string, JsonElement>();
            doc.RootElement.TryGetProperty("values", out JsonElement values);
            foreach (var r in values.EnumerateArray())
            {
                if (!r.TryGetProperty("id", out JsonElement id))
                    break;
                asset.Add(id.GetString(), r);
            }

            foreach (var r in valid.Template.Resources.Values)
            {
                CheckResourceWhatIf(input, result, asset, r);
            }
            foreach (var copy in valid.Template.Copys.Values)
            {
                foreach (var r in copy.Resources.Values)
                {
                    CheckResourceWhatIf(input, result, asset, r);
                }
            }
            if (input.Mode == DeploymentMode.Complete)
            {
                foreach (var item in asset)
                {
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Delete,
                        ResourceId = item.Key
                    });
                }
            }
            else
            {
                foreach (var item in asset)
                {
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Ignore,
                        ResourceId = item.Key
                    });
                }
            }

            result.Status = "succeeded";
            return result;
        }

        private void CheckResourceWhatIf(PredictTemplateOrchestrationInput input, WhatIfOperationResult result, Dictionary<string, JsonElement> asset, Resource resource)
        {
            if (asset.TryGetValue(resource.ResouceId, out JsonElement r))
            {
                if (input.ResultFormat == WhatIfResultFormat.ResourceIdOnly)
                {
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Deploy
                    });
                }
                else
                {
                    // TODO: support WhatIfResultFormat.FullResourcePayloads
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Modify
                    });
                }

                asset.Remove(resource.ResouceId);
            }
            else
            {
                result.Changes.Add(new WhatIfChange()
                {
                    ChangeType = ChangeType.Create,
                    ResourceId = resource.ResouceId
                });
            }
            foreach (var childResource in resource.Resources)
            {
                CheckResourceWhatIf(input, result, asset, childResource);
            }
        }

        public (bool Result, string Message, Copy Copy) ParseCopy(string jsonString, Dictionary<string, object> context)
        {
            var copy = new Copy();
            var deployContext = context["armcontext"] as DeploymentContext;
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

            copy.Id = $"subscription/{deployContext.SubscriptionId}/{this.infrastructure.BuitinServiceTypes.Deployments}/{deployContext.DeploymentName}/copy/{copy.Name}";
            return (true, string.Empty, copy);
        }

        public (bool Result, string Message, Resource resource) ParseResource(
            string jsonString,
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
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(functions.Evaluate(item.GetString(), context).ToString());
                }
            }
            if (root.TryGetProperty("properties", out JsonElement properties))
                r.Properties = properties.ExpandObject(context, this);
            if (root.TryGetProperty("sku", out JsonElement sku))
                r.SKU = sku.GetRawText();
            if (root.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetRawText();
            if (root.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetRawText();
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
            if (t.DeployLevel == DeployLevel.ResourceGroup)
                r.ResouceId = functions.resourceId(
                    deploymentContext,
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.Type,
                    r.Name);
            else if (t.DeployLevel == DeployLevel.Subscription)
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

            foreach (var item in this.infrastructure.ExtensionResources)
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