using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public (bool Result, string Message, DeploymentOrchestrationInput Deployment) ParseDeployment(DeploymentOrchestrationInput input)
        {
            if (string.IsNullOrEmpty(input.Template))
                return (false, "can not find template node", null);
            using JsonDocument doc = JsonDocument.Parse(input.Template);
            var root = doc.RootElement;
            if (!root.TryGetProperty("$schema", out JsonElement schema))
                return (false, "not find $schema in template", null);
            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                return (false, "not find contentVersion in template", null);
            if (!root.TryGetProperty("resources", out JsonElement resources))
                return (false, "not find resources in template", null);
            var template = input.TemplateOjbect;

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
                ParametersLink = input.ParametersLink,
                DeploymentName = input.Name
            };

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
                    var copyResult = ExpandCopyResource(resource, armContext);
                    if (copyResult.Result)
                    {
                        foreach (var item in copyResult.Resources)
                        {
                            if (!template.Resources.TryAdd(item.Name, item))
                                return (false, $"duplicate resource name[{item.Name}] find", null);
                        }
                    }
                    else
                        return (false, copyResult.Message, null);
                }
                else
                {
                    var resResult = ParseResource(resource, armContext);
                    if (!resResult.Result)
                        return (false, resResult.Message, null);
                    foreach (var item in resResult.Resources)
                    {
                        template.Resources.Add(item.Name, item);
                    }
                    foreach (var d in resResult.deployments)
                    {
                        input.Deployments.Add(d);
                    }
                }
            }
            string key;
            Resource tempR;
            foreach (var res in template.Resources.Values)
            {
                if (!res.Condition)
                    continue;
                for (int i = res.DependsOn.Count - 1; i >= 0; i--)
                {
                    key = res.DependsOn[i];
                    tempR = template.Resources[key];
                    if (!tempR.Condition)
                        res.DependsOn.RemoveAt(i);
                    // TODO: check circular dependencies
                }
            }
            return (true, string.Empty, input);
        }

        public WhatIfOperationResult WhatIf(PredictTemplateOrchestrationInput input)
        {
            var result = new WhatIfOperationResult();
            var valid = ParseDeployment(new DeploymentOrchestrationInput()
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

                TemplateLink = input.TemplateLink,
                ParametersLink = input.ParametersLink
            };
            string queryScope;
            if (input.ScopeType == ScopeType.ResourceGroup)
                queryScope = $"subscriptions/{input.SubscriptionId}/resourceGroups/{input.ResourceGroupName}";
            else
                queryScope = $"subscriptions/{input.SubscriptionId}";
            var str = this.infrastructure.List(deploymentContext, queryScope, valid.Deployment.TemplateOjbect.ApiProfile, string.Empty, "resources");
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

            foreach (var r in valid.Deployment.TemplateOjbect.Resources.Values)
            {
                CheckResourceWhatIf(input, result, asset, r);
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
            if (asset.TryGetValue(resource.Name, out JsonElement r))
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

                asset.Remove(resource.Name);
            }
            else
            {
                result.Changes.Add(new WhatIfChange()
                {
                    ChangeType = ChangeType.Create,
                    ResourceId = resource.ResouceId
                });
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

        public (bool Result, string Message, DeploymentOrchestrationInput Deployment) ParseDeployment(
           Resource resource,
           DeploymentContext deploymentContext)
        {
            var armContext = new Dictionary<string, object>() { { "armcontext", deploymentContext } };
            using var doc = JsonDocument.Parse(resource.Properties);
            var rootElement = doc.RootElement;

            var mode = DeploymentMode.Incremental;
            if (rootElement.TryGetProperty("mode", out JsonElement _mode)
                && _mode.GetString().Equals(DeploymentMode.Complete.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                mode = DeploymentMode.Complete;
            }
            string template = string.Empty;
            if (rootElement.TryGetProperty("template", out JsonElement _template))
            {
                template = _template.GetRawText();
            }
            TemplateLink templateLink = null;
            if (rootElement.TryGetProperty("templateLink", out JsonElement _templateLink))
            {
                templateLink = new TemplateLink()
                {
                    ContentVersion = _templateLink.GetProperty("contentVersion").GetString(),
                    Uri = this.functions.Evaluate(_templateLink.GetProperty("uri").GetString(), armContext).ToString()
                };
            }
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#scope-for-expressions-in-nested-templates
            string parameters = string.Empty;
            ParametersLink parametersLink = null;
            if (rootElement.TryGetProperty("expressionEvaluationOptions", out JsonElement _expressionEvaluationOptions)
                && _expressionEvaluationOptions.GetProperty("scope").GetString().Equals("inner", StringComparison.OrdinalIgnoreCase))
            {
                if (rootElement.TryGetProperty("parameters", out JsonElement _parameters))
                {
                    parameters = _parameters.GetRawText();
                }
                if (rootElement.TryGetProperty("parametersLink", out JsonElement _parametersLink))
                {
                    parametersLink = new ParametersLink()
                    {
                        ContentVersion = _parametersLink.GetProperty("contentVersion").GetString(),
                        Uri = this.functions.Evaluate(_parametersLink.GetProperty("uri").GetString(), armContext).ToString()
                    };
                }
            }
            else
            {
                parameters = deploymentContext.Parameters;
                using MemoryStream ms = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
                writer.WriteStartObject();
                using var doc1 = JsonDocument.Parse(template);
                var root1 = doc1.RootElement;
                foreach (var node in root1.EnumerateObject())
                {
                    writer.WritePropertyName(node.Name);
                    if (node.Name.Equals("parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        using var p = JsonDocument.Parse(deploymentContext.Template.Parameters);
                        p.RootElement.WriteTo(writer);
                    }
                    else if (node.Name.Equals("variables", StringComparison.OrdinalIgnoreCase))
                    {
                        using var v = JsonDocument.Parse(deploymentContext.Template.Variables);
                        v.RootElement.WriteTo(writer);
                    }
                    else
                    {
                        node.Value.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
                writer.Flush();
                template = Encoding.UTF8.GetString(ms.ToArray());
            }

            var deployInput = new DeploymentOrchestrationInput()
            {
                CorrelationId = deploymentContext.CorrelationId,
                Name = resource.Name,
                SubscriptionId = resource.SubscriptionId,
                ResourceGroup = resource.ResourceGroup,
                Mode = mode,
                Template = template,
                TemplateLink = templateLink,
                Parameters = parameters,
                ParametersLink = parametersLink,
                ApiVersion = resource.ApiVersion
            };
            var t = ParseDeployment(deployInput);
            if (!t.Result)
                return (false, t.Message, null);
            return (true, string.Empty, t.Deployment);
        }

        public (bool Result, string Message, List<Resource> Resources, List<DeploymentOrchestrationInput> deployments)
            ParseResource(
            JsonElement resourceElement,
            Dictionary<string, object> context,
            string parentName = "",
            string parentType = "")
        {
            DeploymentContext deploymentContext = context["armcontext"] as DeploymentContext;
            Resource r = new Resource();
            List<Resource> resources = new List<Resource>();
            List<DeploymentOrchestrationInput> deployments = new List<DeploymentOrchestrationInput>();
            resources.Add(r);
            if (resourceElement.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.False)
                    r.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    r.Condition = (bool)functions.Evaluate(condition.GetString(), context);
            }

            if (resourceElement.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                return (false, "not find apiVersion in resource node", null, null);
            if (resourceElement.TryGetProperty("type", out JsonElement type))
                r.Type = functions.Evaluate(type.GetString(), context).ToString();
            else
                return (false, "not find type in resource node", null, null);
            if (!string.IsNullOrEmpty(parentType))
                r.FullType = $"{parentType}/{r.Type}";
            else
                r.FullType = r.Type;

            if (resourceElement.TryGetProperty("name", out JsonElement name))
                r.Name = functions.Evaluate(name.GetString(), context).ToString();
            else
                return (false, "not find name in resource node", null, null);
            if (!string.IsNullOrEmpty(parentName))
            {
                r.FullName = $"{parentName}/{r.Name}";
                r.DependsOn.Add(parentName);
            }
            else
                r.FullName = r.Name;

            if (resourceElement.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                r.ResourceGroup = functions.Evaluate(resourceGroup.GetString(), context).ToString();
            else
                r.ResourceGroup = deploymentContext.ResourceGroup;
            if (resourceElement.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                r.SubscriptionId = functions.Evaluate(subscriptionId.GetString(), context).ToString();
            else
                r.SubscriptionId = deploymentContext.SubscriptionId;

            if (resourceElement.TryGetProperty("location", out JsonElement location))
                r.Location = functions.Evaluate(location.GetString(), context).ToString();
            if (resourceElement.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();
            if (resourceElement.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(functions.Evaluate(item.GetString(), context).ToString());
                }
            }

            #region ResouceId

            if (deploymentContext.Template.DeployLevel == DeployLevel.ResourceGroup)
                r.ResouceId = functions.resourceId(
                    deploymentContext,
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.FullType,
                    r.FullName);
            else if (deploymentContext.Template.DeployLevel == DeployLevel.Subscription)
                r.ResouceId = functions.subscriptionResourceId(deploymentContext, r.SubscriptionId, r.Type, r.Name);
            else
                r.ResouceId = functions.tenantResourceId(r.Type, r.Name);

            #endregion ResouceId

            if (resourceElement.TryGetProperty("sku", out JsonElement sku))
                r.SKU = sku.GetRawText();
            if (resourceElement.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetString();
            if (resourceElement.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetRawText();

            if (!r.Condition)
                return (true, "Condition equal false", resources, null);

            if (resourceElement.TryGetProperty("properties", out JsonElement properties))
            {
                if (r.FullType == infrastructure.BuitinServiceTypes.Deployments)
                    r.Properties = properties.GetRawText();
                else
                    r.Properties = properties.ExpandObject(context, this);
            }
            if (resourceElement.TryGetProperty("resources", out JsonElement _resources))
            {
                foreach (var childres in _resources.EnumerateArray())
                {
                    //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
                    var childResult = ParseResource(childres, context, r.Name, r.Type);
                    if (childResult.Result)
                    {
                        r.Resources.Add(childResult.Resources[0].Name);
                        resources.AddRange(childResult.Resources);
                    }
                    else
                        return (false, childResult.Message, null, null);
                }
            }
            if (r.FullType == infrastructure.BuitinServiceTypes.Deployments)
            {
                var d = ParseDeployment(r, deploymentContext);
                if (d.Result)
                    deployments.Add(d.Deployment);
                else
                    return (false, d.Message, null, null);
            }

            foreach (var item in this.infrastructure.ExtensionResources)
            {
                if (resourceElement.TryGetProperty(item, out JsonElement e))
                {
                    r.ExtensionResource.Add(item, e.GetRawText());
                }
            }

            return (true, string.Empty, resources, deployments);
        }

        private (bool Result, string Message, List<Resource> Resources) ExpandCopyResource(
           JsonElement resource,
           Dictionary<string, object> context)
        {
            resource.TryGetProperty("copy", out JsonElement _copy);
            var copyResult = ParseCopy(_copy.GetRawText(), context);
            if (!copyResult.Result)
                return (false, copyResult.Message, null);
            var copy = copyResult.Copy;
            DeploymentContext deploymentContext = context["armcontext"] as DeploymentContext;

            Resource CopyResource = new Resource()
            {
                Name = copy.Name,
                Type = Copy.ServiceType,
                FullName = $"{deploymentContext.DeploymentName}/{copy.Name}",
                FullType = $"{infrastructure.BuitinServiceTypes.Deployments}/{Copy.ServiceType}",
                ResouceId = $"{infrastructure.BuitinServiceTypes.Deployments}/{deploymentContext.DeploymentName}/{Copy.ServiceType}/{copy.Name}"
            };
            List<Resource> resources = new List<Resource>();
            resources.Add(CopyResource);

            var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>();
            copyContext.Add("armcontext", context["armcontext"]);
            copyContext.Add("copyindex", copyindex);
            copyContext.Add("currentloopname", copy.Name);

            for (int i = 0; i < copy.Count; i++)
            {
                copyindex[copy.Name] = i;
                var r = ParseResource(resource, copyContext);
                if (r.Result)
                {
                    CopyResource.Resources.Add(r.Resources[0].Name);
                    resources.AddRange(r.Resources);
                    if (copy.Mode == Copy.SerialMode
                        && copy.BatchSize > 0
                        && i >= copy.BatchSize)
                    {
                        r.Resources[0].DependsOn.Add(CopyResource.Resources[i - copy.BatchSize]);
                    }
                }
                else
                    return (false, r.Message, null);
            }
            return (true, copy.Name, resources);
        }
    }
}