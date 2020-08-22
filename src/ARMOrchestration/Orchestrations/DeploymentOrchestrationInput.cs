using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace maskx.ARMOrchestration.Orchestrations
{
    [JsonObject(MemberSerialization.OptOut)]
    public class DeploymentOrchestrationInput
    {
        public bool IsRetry;

        public (bool, string) Validate(IServiceProvider service = null)
        {
            if (service != null)
                this.ServiceProvider = service;
            if (this.ServiceProvider == null)
                throw new Exception("validate template need ServiceProvider");
            return this.Template.Validate();
        }

        public static DeploymentOrchestrationInput Parse(Resource resource,
                  DeploymentOrchestrationInput deploymentContext,
                  ARMFunctions functions,
                  IInfrastructure infrastructure)
        {
            Dictionary<string, object> context = new Dictionary<string, object>();
            foreach (var item in resource.FullContext)
            {
                context.Add(item.Key, item.Value);
            }

            using var doc = JsonDocument.Parse(resource.RawProperties.RawString);
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
                    Uri = functions.Evaluate(_templateLink.GetProperty("uri").GetString(), context).ToString()
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
                        Uri = functions.Evaluate(_parametersLink.GetProperty("uri").GetString(), context).ToString()
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
                if (deploymentContext.Template.Variables != null)
                {
                    using var v = JsonDocument.Parse(deploymentContext.Template.Variables.ToString());
                    writer.WritePropertyName("variables");
                    v.RootElement.WriteTo(writer);
                }

                writer.WriteEndObject();
                writer.Flush();
                template = Encoding.UTF8.GetString(ms.ToArray());
            }
            var (groupId, groupType, hierarchyId) = infrastructure.GetGroupInfo(resource.ManagementGroupId, resource.SubscriptionId, resource.ResourceGroup);
            context.Remove(ContextKeys.ARM_CONTEXT);
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
                Template = template,
                TemplateLink = templateLink,
                Parameters = parameters,
                ParametersLink = parametersLink,
                ApiVersion = resource.ApiVersion,
                CreateByUserId = deploymentContext.CreateByUserId,
                LastRunUserId = deploymentContext.LastRunUserId,
                DependsOn = resource.DependsOn,
                Extensions = deploymentContext.Extensions,
                TenantId = deploymentContext.TenantId,
                Context = context,
                ServiceProvider = resource.ServiceProvider
            };

            return deployInput;
        }

        private IServiceProvider _ServiceProvider;

        [JsonIgnore]
        public IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                _ServiceProvider = value;
                if (Template != null)
                    Template.Input = this;
            }
        }

        public bool IsRuntime { get; set; } = false;

        /// <summary>
        /// group Id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// group type, such as resource group, subscription, management group
        /// </summary>
        public string GroupType { get; set; }

        /// <summary>
        /// include the information from root to this one
        /// </summary>
        public string HierarchyId { get; set; }

        /// <summary>
        /// the deploymentId of root deployment
        /// </summary>
        public string RootId { get; set; }

        /// <summary>
        /// the deploymentId of parent, DependsOn will search resource status in this deployment's scope
        /// </summary>
        public string ParentId { get; set; }

        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string DeploymentName { get; set; }

        public string ResourceGroup { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ManagementGroupId { get; set; }

        /// <summary>
        /// Complete  or Incremental
        /// </summary>
        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;

        private Template template = null;

        public Template Template
        {
            get
            {
                if (template == null && TemplateLink != null)
                {
                    // get template from templateLink
                }
                return template;
            }
            set
            {
                template = value;
                template.Input = this;
            }
        }

        public string Parameters { get; set; }
        public string ApiVersion { get; set; }

        /// <summary>
        /// When a deployment fails, you can automatically redeploy an earlier, successful deployment from your deployment history. This functionality is useful if you've got a known good state for your infrastructure deployment and want to revert to this state.
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error#rest-api"/>
        public bool OnErrorDeployment { get; set; } = false;

        /// <summary>
        /// the user id of create this deployment
        /// </summary>
        public string CreateByUserId { get; set; }

        /// <summary>
        /// the user Id of last run this deployment
        /// </summary>
        public string LastRunUserId { get; set; }

        /// <summary>
        /// some extension settings
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; }

        public TemplateLink TemplateLink { get; set; }
        public ParametersLink ParametersLink { get; set; }

        public DependsOnCollection DependsOn { get; set; } = new DependsOnCollection();

        private Dictionary<string, DeploymentOrchestrationInput> _Deployments;

        [JsonIgnore]
        public Dictionary<string, DeploymentOrchestrationInput> Deployments
        {
            get
            {
                if (_Deployments == null)
                {
                    var infra = ServiceProvider.GetService<IInfrastructure>();
                    var func = ServiceProvider.GetService<ARMFunctions>();
                    _Deployments = new Dictionary<string, DeploymentOrchestrationInput>();
                    foreach (var r in this.Template.Resources)
                    {
                        if (r.Type == infra.BuiltinServiceTypes.Deployments)
                        {
                            _Deployments.Add(r.Name, Parse(r, this, func, infra));
                        }
                    }
                }
                return _Deployments;
            }
        }

        public IEnumerable<DeploymentOrchestrationInput> EnumerateDeployments()
        {
            foreach (var d in this.Deployments.Values)
            {
                yield return d;
                foreach (var nest in d.EnumerateDeployments())
                {
                    yield return nest;
                }
            }
        }

        /// <summary>
        ///  all resource in this Deployment
        /// </summary>
        public IEnumerable<Resource> EnumerateResource()
        {
            foreach (var r in this.Template.Resources)
            {
                yield return r;
            }
            foreach (var d in this.Deployments.Values)
            {
                foreach (var r in d.EnumerateResource())
                {
                    yield return r;
                }
            }
        }

        public string GetResourceId(IInfrastructure infrastructure)
        {
            string resourceId = string.Empty;
            if (!string.IsNullOrEmpty(this.SubscriptionId))
                resourceId = $"/{infrastructure.BuiltinPathSegment.Subscription}/{this.SubscriptionId}";
            if (!string.IsNullOrEmpty(this.ManagementGroupId))
                resourceId = $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{this.ManagementGroupId}";
            if (!string.IsNullOrEmpty(this.ResourceGroup))
                resourceId += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{this.ResourceGroup}";
            resourceId += $"/{infrastructure.BuiltinPathSegment.Provider}/{infrastructure.BuiltinServiceTypes.Deployments}/{this.DeploymentName}";
            return resourceId;
        }

        public Dictionary<string, object> Context = new Dictionary<string, object>();
    }
}