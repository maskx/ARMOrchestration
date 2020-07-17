using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestrationInput : DeploymentContext
    {
        public static DeploymentOrchestrationInput Validate(DeploymentOrchestrationInput input, ARMFunctions functions, IInfrastructure infrastructure)
        {
            if (input.Template != null)
                return input;
            input.Template = Template.Parse(input.TemplateContent, input, functions, infrastructure);
            foreach (var res in input.Template.Resources.Values)
            {
                #region Deployment
                if (res.FullType == infrastructure.BuiltinServiceTypes.Deployments)
                {
                    var deploy = Parse(res, input, functions, infrastructure);
                    input.Deployments.Add(deploy.DeploymentName, deploy);
                }
                #endregion

                #region dependsOn
                for (int i = res.DependsOn.Count - 1; i >= 0; i--)
                {
                    string dependsOnName = res.DependsOn[i];
                    // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
                    // When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
                    if (!input.Template.Resources.ContainsKey(dependsOnName))
                    {
                        if (input.Template.ConditionFalseResources.Contains(dependsOnName))
                            res.DependsOn.RemoveAt(i);
                        else
                            throw new Exception($"cannot find dependson resource named '{dependsOnName}'");
                    }
                }
                // TODO: check circular dependencies
                #endregion
            }
            return input;
        }

        public static DeploymentOrchestrationInput Parse(Resource resource,
                  DeploymentContext deploymentContext,
                  ARMFunctions functions,
                  IInfrastructure infrastructure)
        {
            var armContext = new Dictionary<string, object>() {
                { ContextKeys.ARM_CONTEXT, deploymentContext },
                {ContextKeys.IS_PREPARE,true }
            };
            using var doc = JsonDocument.Parse(resource.Properties);
            var rootElement = doc.RootElement;

            var mode = DeploymentMode.Incremental;
            if (rootElement.TryGetProperty("mode", out JsonElement _mode))
            {
                if (_mode.GetString().Equals(DeploymentMode.Complete.ToString(), StringComparison.OrdinalIgnoreCase))
                    mode = DeploymentMode.Complete;
                if (_mode.GetString().Equals(DeploymentMode.OnlyCreation.ToString(), StringComparison.OrdinalIgnoreCase))
                    mode = DeploymentMode.OnlyCreation;
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
                    Uri = functions.Evaluate(_templateLink.GetProperty("uri").GetString(), armContext).ToString()
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
                        Uri = functions.Evaluate(_parametersLink.GetProperty("uri").GetString(), armContext).ToString()
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
            var (groupId, groupType, hierarchyId) = infrastructure.GetGroupInfo(resource.ManagementGroupId, resource.SubscriptionId, resource.ResourceGroup);
            var deployInput = new DeploymentOrchestrationInput()
            {
                RootId = deploymentContext.RootId,
                DeploymentId = Guid.NewGuid().ToString("N"),
                ParentId = deploymentContext.GetResourceId(infrastructure),
                GroupId = groupId,
                GroupType = groupType,
                HierarchyId = hierarchyId,
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
                LastRunUserId = deploymentContext.LastRunUserId,
                DependsOn = resource.DependsOn,
                Extensions = deploymentContext.Extensions,
                TenantId = deploymentContext.TenantId
            };
            return Validate(deployInput, functions, infrastructure);
        }
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, DeploymentOrchestrationInput> Deployments { get; set; } = new Dictionary<string, DeploymentOrchestrationInput>();
    }
}