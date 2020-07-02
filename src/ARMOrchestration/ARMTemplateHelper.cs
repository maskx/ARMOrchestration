using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        public readonly ARMFunctions ARMfunctions;
        private readonly IInfrastructure infrastructure;

        private readonly string _saveDeploymentOperationCommandString;

        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions functions,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.ARMfunctions = functions;
            this.infrastructure = infrastructure;

            this._saveDeploymentOperationCommandString = string.Format(@"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@ExecutionId)) as [Source](InstanceId,ExecutionId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Result],[Comments],[CreateByUserId],[LastRunUserId])
	VALUES
	(@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,@Input,@Result,@Comments,@CreateByUserId,@LastRunUserId)
WHEN MATCHED THEN
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(@Result,[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId);
", this.options.Database.DeploymentOperationsTableName);
        }

        // TODO: when this is a async task, in orchestration await this will make orchestration cannot be completed, need investigation
        public void SaveDeploymentOperation(DeploymentOperation deploymentOperation)
        {
            TraceActivityEventSource.Log.TraceEvent(
                TraceEventType.Information,
                "DeploymentOperationsActivity",
               deploymentOperation.InstanceId,
                deploymentOperation.ExecutionId,
                $"{deploymentOperation.ResourceId}-{deploymentOperation.Stage}",
                deploymentOperation.Input,
                deploymentOperation.Stage.ToString());

            using var db = new DbAccess(this.options.Database.ConnectionString);
            db.AddStatement(this._saveDeploymentOperationCommandString, deploymentOperation);
            db.ExecuteNonQueryAsync().Wait();
        }

        public (bool Result, string Message, DeploymentOrchestrationInput Deployment) ParseDeployment(DeploymentOrchestrationInput input)
        {
            string templateContent = string.Empty;
            if (input.Template != null)
                return (true, "", input);
            Template template = new Template();
            input.Template = template;
            if (!string.IsNullOrEmpty(input.TemplateContent))
            {
                templateContent = input.TemplateContent;
            }
            else if (input.TemplateLink != null)
            {
                // TODO: get template content form link
                templateContent = input.TemplateContent;
            }
            if (string.IsNullOrEmpty(templateContent))
                return (false, "can not find template setting", null);
            using JsonDocument doc = JsonDocument.Parse(templateContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("$schema", out JsonElement schema))
                return (false, "not find $schema in template", null);
            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                return (false, "not find contentVersion in template", null);
            if (!root.TryGetProperty("resources", out JsonElement resources))
                return (false, "not find resources in template", null);

            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                {ContextKeys.ARM_CONTEXT, input},
                {ContextKeys.IS_PREPARE,true }
            };

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
                var fr = ARMTemplate.Functions.Parse(functions.GetRawText());
                if (fr.Result)
                    template.Functions = fr.Functions;
                else
                    return (false, fr.Message, null);
            }
            string error = string.Empty;
            foreach (var resource in resources.EnumerateArray())
            {
                bool Result;
                string Message;
                List<Resource> Resources;
                List<DeploymentOrchestrationInput> deployments = null;
                if (resource.TryGetProperty("copy", out JsonElement copy))
                    (Result, Message, Resources) = ExpandCopyResource(resource, armContext);
                else
                    (Result, Message, Resources, deployments) = ParseResource(resource, armContext);
                if (!Result)
                {
                    error += Message + Environment.NewLine;
                    break;
                }
                foreach (var item in Resources)
                {
                    if (!item.Condition)
                        template.ConditionFalseResources.Add(item.Name);
                    else if (!template.Resources.TryAdd(item.Name,item))
                    {
                        error += $"duplicate resource name[{item.Name}] find" + Environment.NewLine;
                        break;
                    }
                }
                if (deployments != null)
                {
                    foreach (var d in deployments)
                    {
                        if (!input.Deployments.TryAdd(d.DeploymentName, d))
                        {
                            error += $"duplicate resource name[{d.DeploymentName}] find" + Environment.NewLine;
                            break;
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(error))
                return (false, error, null);
            string dependsOnName = string.Empty;
            foreach (var res in template.Resources.Values)
            {
                for (int i = res.DependsOn.Count - 1; i >= 0; i--)
                {
                    dependsOnName = res.DependsOn[i];
                    // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
                    // When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
                    if (!template.Resources.ContainsKey(dependsOnName))
                    {
                        if (template.ConditionFalseResources.Contains(dependsOnName))
                            res.DependsOn.RemoveAt(i);
                        else
                            throw new Exception($"cannot find dependson resource named '{dependsOnName}'");
                    }
                }
                // TODO: check circular dependencies
            }
            return (true, string.Empty, input);
        }

        public WhatIfOperationResult WhatIf(PredictTemplateOrchestrationInput input)
        {
            var result = new WhatIfOperationResult();
            var (Result, Message, Deployment) = ParseDeployment(new DeploymentOrchestrationInput()
            {
                CorrelationId = input.CorrelationId,
                Parameters = input.Parameters,
                ResourceGroup = input.ResourceGroupName,
                SubscriptionId = input.SubscriptionId,
                TemplateContent = input.Template,
                TenantId = input.TenantId
            });
            if (!Result)
            {
                result.Status = "failed";
                result.Error = new ErrorResponse() { Code = "400", Message = Message };
            }
            DeploymentContext deploymentContext = new DeploymentContext()
            {
                CorrelationId = input.CorrelationId,
                Mode = input.Mode,
                ResourceGroup = input.ResourceGroupName,
                SubscriptionId = input.SubscriptionId,
                TenantId = input.TenantId,
                Parameters = input.Parameters
            };
            string queryScope = $"/{infrastructure.BuiltinPathSegment.Subscription}/{input.SubscriptionId}";
            if (input.ScopeType == ScopeType.ResourceGroup)
                queryScope += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{input.ResourceGroupName}";
            var str = this.infrastructure.List(deploymentContext, queryScope, Deployment.Template.ApiProfile, string.Empty, "resources");
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

            foreach (var r in Deployment.Template.Resources.Values)
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
            var deployContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;
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
                    copy.Count = (int)ARMfunctions.Evaluate(count.GetString(), context);
                else
                    return (false, "the value of count property should be Number in copy node", null);
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                if (context.ContainsKey(ContextKeys.DEPENDSON))
                    return (false, "You can't use the reference function to set the value of the count property in a copy loop.", null);
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
                    copy.BatchSize = (int)ARMfunctions.Evaluate(batchSize.GetString(), context);
            }
            if (root.TryGetProperty("input", out JsonElement input))
            {
                copy.Input = input.GetRawText();
            }
            if (!string.IsNullOrEmpty(deployContext.SubscriptionId))
                copy.Id = $"/{infrastructure.BuiltinPathSegment.Subscription}/{deployContext.SubscriptionId}";
            if (!string.IsNullOrEmpty(deployContext.ManagementGroupId))
                copy.Id = $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{deployContext.ManagementGroupId}";
            if (!string.IsNullOrEmpty(deployContext.ResourceGroup))
                copy.Id += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{deployContext.ResourceGroup}";
            copy.Id += $"/{this.infrastructure.BuitinServiceTypes.Deployments}/{deployContext.DeploymentName}/{infrastructure.BuitinServiceTypes.Copy}/{copy.Name}";
            return (true, string.Empty, copy);
        }

        public (bool Result, string Message, DeploymentOrchestrationInput Deployment) ParseDeployment(
           Resource resource,
           DeploymentContext deploymentContext)
        {
            var armContext = new Dictionary<string, object>() {
                { ContextKeys.ARM_CONTEXT, deploymentContext },
                {ContextKeys.IS_PREPARE,true }
            };
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
                    Uri = this.ARMfunctions.Evaluate(_templateLink.GetProperty("uri").GetString(), armContext).ToString()
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
                        Uri = this.ARMfunctions.Evaluate(_parametersLink.GetProperty("uri").GetString(), armContext).ToString()
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
                    if (node.Name.Equals("parameters", StringComparison.OrdinalIgnoreCase)
                        || node.Name.Equals("variables", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    writer.WritePropertyName(node.Name);
                    node.Value.WriteTo(writer);
                }
                if (!string.IsNullOrWhiteSpace(deploymentContext.Template.Parameters))
                {
                    using var p = JsonDocument.Parse(deploymentContext.Template.Parameters);
                    writer.WritePropertyName("parameters");
                    p.RootElement.WriteTo(writer);
                }
                if (!string.IsNullOrWhiteSpace(deploymentContext.Template.Variables))
                {
                    using var v = JsonDocument.Parse(deploymentContext.Template.Variables);
                    writer.WritePropertyName("variables");
                    v.RootElement.WriteTo(writer);
                }

                writer.WriteEndObject();
                writer.Flush();
                template = Encoding.UTF8.GetString(ms.ToArray());
            }

            var deployInput = new DeploymentOrchestrationInput()
            {
                RootId = deploymentContext.RootId,
                DeploymentId = Guid.NewGuid().ToString("N"),
                ParentId = deploymentContext.DeploymentId,
                GroupId = deploymentContext.GroupId,
                GroupType = deploymentContext.GroupType,
                HierarchyId = deploymentContext.HierarchyId,
                CorrelationId = deploymentContext.CorrelationId,
                SubscriptionId = resource.SubscriptionId,
                ManagementGroupId = resource.ManagementGroupId,
                ResourceGroup = resource.ResourceGroup,
                DeploymentName = resource.Name,
                Mode = mode,
                TemplateContent = template,
                TemplateLink = templateLink,
                Parameters = parameters,
                ParametersLink = parametersLink,
                ApiVersion = resource.ApiVersion,
                CreateByUserId = deploymentContext.CreateByUserId,
                LastRunUserId = deploymentContext.LastRunUserId
            };

            var (Result, Message, Deployment) = ParseDeployment(deployInput);
            if (!Result)
                return (false, Message, null);
            return (true, string.Empty, Deployment);
        }

        private bool HandleDependsOn(Resource r, Dictionary<string, object> context)
        {
            if (context.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
            {
                r.DependsOn.AddRange(conditionDep as List<string>);
                context.Remove(ContextKeys.DEPENDSON);
            }
            if (context.ContainsKey(ContextKeys.NEED_REEVALUATE))
            {
                context.Remove(ContextKeys.NEED_REEVALUATE);
                return true;
            }
            return false;
        }

        public (bool Result, string Message, List<Resource> Resources, List<DeploymentOrchestrationInput> deployments)
            ParseResource(
            JsonElement resourceElement,
            Dictionary<string, object> context,
            string parentName = "",
            string parentType = "")
        {

            DeploymentContext deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;
            Resource r = new Resource();
            List<Resource> resources = new List<Resource>();
            List<DeploymentOrchestrationInput> deployments = new List<DeploymentOrchestrationInput>();

            if (resourceElement.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.False)
                    r.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    r.Condition = (bool)ARMfunctions.Evaluate(condition.GetString(), context);
            }

            if (resourceElement.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                return (false, "not find apiVersion in resource node", null, null);
            if (resourceElement.TryGetProperty("type", out JsonElement type))
                r.Type = type.GetString();
            else
                return (false, "not find type in resource node", null, null);
            if (!string.IsNullOrEmpty(parentType))
                r.FullType = $"{parentType}/{r.Type}";
            else
                r.FullType = r.Type;
            if (resourceElement.TryGetProperty("name", out JsonElement name))
                r.Name = ARMfunctions.Evaluate(name.GetString(), context).ToString();
            else
                return (false, "not find name in resource node", null, null);
            if (!string.IsNullOrEmpty(parentName))
                r.FullName = $"{parentName}/{r.Name}";
            else
                r.FullName = r.Name;

            if (resourceElement.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
            {
                r.ResourceGroup = ARMfunctions.Evaluate(resourceGroup.GetString(), context).ToString();
            }
            else
                r.ResourceGroup = deploymentContext.ResourceGroup;
            if (resourceElement.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                r.SubscriptionId = ARMfunctions.Evaluate(subscriptionId.GetString(), context).ToString();
            else
                r.SubscriptionId = deploymentContext.SubscriptionId;
            // TODO: need support deployment resource in managementGroup
            // subscriptionId and managementGroupId should be only one have value
            if (resourceElement.TryGetProperty("managementGroupId", out JsonElement managementGroupId))
                r.ManagementGroupId = ARMfunctions.Evaluate(managementGroupId.GetString(), context).ToString();
            else
                r.ManagementGroupId = deploymentContext.ManagementGroupId;
            if (resourceElement.TryGetProperty("location", out JsonElement location))
                r.Location = ARMfunctions.Evaluate(location.GetString(), context).ToString();

            if (resourceElement.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();
            if (resourceElement.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(ARMfunctions.Evaluate(item.GetString(), context).ToString());
                }
            }

            #region ResouceId

            if (deploymentContext.Template.DeployLevel == DeployLevel.ResourceGroup)
            {
                List<object> pars = new List<object>
                {
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = ARMfunctions.ResourceId(
                   deploymentContext,
                   pars.ToArray());
            }
            else if (deploymentContext.Template.DeployLevel == DeployLevel.Subscription)
            {
                List<object> pars = new List<object>
                {
                    r.SubscriptionId,
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = ARMfunctions.SubscriptionResourceId(deploymentContext, pars.ToArray());
            }
            else
            {
                List<object> pars = new List<object>
                {
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = ARMfunctions.TenantResourceId(pars.ToArray());
            }

            #endregion ResouceId

            if (resourceElement.TryGetProperty("sku", out JsonElement sku))
                r.SKU = SKU.Parse(sku.GetRawText(), ARMfunctions, context);
            else
                r.SKU = new SKU() { Name = SKU.Default };
            if (resourceElement.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetString();
            if (resourceElement.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetRawText();
            if (resourceElement.TryGetProperty("zones", out JsonElement zones))
            {
                foreach (var z in zones.EnumerateArray())
                {
                    r.Zones.Add(ARMfunctions.Evaluate(z.GetString(), context).ToString());
                }
            }
            if (context.ContainsKey(ContextKeys.DEPENDSON))
            {
                //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                return (false, "The reference function can only be used in the properties of a resource definition and the outputs section of a template or deployment.", resources, deployments);
            }
            resources.Add(r);
            if (!r.Condition)
                return (true, "Condition equal false", resources, deployments);

            if (resourceElement.TryGetProperty("properties", out JsonElement properties))
            {
                if (r.FullType == infrastructure.BuitinServiceTypes.Deployments)
                    r.Properties = properties.GetRawText();
                else
                {
                    r.Properties = properties.ExpandObject(context, this);
                    // if there has Implicit dependency by reference function in properties
                    // the reference function should be evaluate at provisioning time
                    // so keep the original text
                    if (HandleDependsOn(r, context))
                        r.Properties = properties.GetRawText();
                }
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
                var (Result, Message, Deployment) = ParseDeployment(r, deploymentContext);
                if (Result)
                    deployments.Add(Deployment);
                else
                    return (false, Message, null, null);
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
            DeploymentContext deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;

            Resource CopyResource = new Resource()
            {
                Name = copy.Name,
                Type = Copy.ServiceType,
                FullName = $"{deploymentContext.DeploymentName}/{copy.Name}",
                FullType = $"{infrastructure.BuitinServiceTypes.Deployments}/{Copy.ServiceType}",
                ResouceId = $"{infrastructure.BuitinServiceTypes.Deployments}/{deploymentContext.DeploymentName}/{Copy.ServiceType}/{copy.Name}"
            };
            List<Resource> resources = new List<Resource>
            {
                CopyResource
            };

            var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>
            {
                { ContextKeys.ARM_CONTEXT, deploymentContext },
                { ContextKeys.COPY_INDEX, copyindex },
                { ContextKeys.CURRENT_LOOP_NAME, copy.Name },
                { ContextKeys.IS_PREPARE, true }
            };

            for (int i = 0; i < copy.Count; i++)
            {
                copyindex[copy.Name] = i;
                var (Result, Message, Resources, deployments) = ParseResource(resource, copyContext);
                if (Result)
                {
                    Resources[0].CopyIndex = i;
                    Resources[0].CopyId = copy.Id;
                    Resources[0].CopyName = copy.Name;
                    CopyResource.Resources.Add(Resources[0].Name);
                    resources.AddRange(Resources);
                    if (copy.Mode == Copy.SerialMode
                        && copy.BatchSize > 0
                        && i >= copy.BatchSize)
                    {
                        Resources[0].DependsOn.Add(CopyResource.Resources[i - copy.BatchSize]);
                    }
                }
                else
                    return (false, Message, null);
            }
            return (true, copy.Name, resources);
        }

        public string ExpadResourceProperties(Resource resource, DeploymentContext deploymentContext)
        {
            if (string.IsNullOrEmpty(resource.Properties))
                return string.Empty;
            {
                var doc = JsonDocument.Parse(resource.Properties);
                Dictionary<string, object> cxt = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, deploymentContext } };
                if (!string.IsNullOrEmpty(resource.CopyName))
                {
                    cxt.Add(ContextKeys.CURRENT_LOOP_NAME, resource.CopyName);
                    cxt.Add(ContextKeys.COPY_INDEX, new Dictionary<string, int>() { { resource.CopyName, resource.CopyIndex } });
                }
                return doc.RootElement.ExpandObject(cxt, this);
            }
        }
    }
}