using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#microsoftresourcesdeployments-object
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Deployment
    {
        // TODO: support retry
        public bool IsRetry;

        public (bool, string) Validate(IServiceProvider service = null)
        {
            if (service != null)
                this.ServiceProvider = service;
            if (this.ServiceProvider == null)
                throw new Exception("validate template need ServiceProvider");
          
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-syntax#parameters
            using JsonDocument parDefine = JsonDocument.Parse(template.Parameters);
            if (parDefine.RootElement.EnumerateObject().Count() > 0)
            {
                using JsonDocument ParValue = JsonDocument.Parse(string.IsNullOrEmpty(this.Parameters) ? "{}" : this.Parameters);
                var root = ParValue.RootElement;

                var func = ServiceProvider.GetService<ARMFunctions>();
                var context = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, this } };

                foreach (var par in parDefine.RootElement.EnumerateObject())
                {
                    if (!root.TryGetProperty(par.Name, out JsonElement p))
                    {
                        if (!par.Value.TryGetProperty("defaultValue", out JsonElement defaultV))
                            return (false, $"must set the parameter value of {par.Name}");
                    }
                    else
                    {
                        #region check type
                        var type = par.Value.GetProperty("type").GetString();
                        if (type == "string" && p.ValueKind != JsonValueKind.String)
                            return (false, $"{par.Name} parameter need a string value");
                        if (type == "int" && p.ValueKind != JsonValueKind.Number)
                            return (false, $"{par.Name} parameter need a int value");
                        if (type == "bool" && p.ValueKind != JsonValueKind.True && p.ValueKind != JsonValueKind.False)
                            return (false, $"{par.Name} parameter need a bool value");
                        if (type == "object" && p.ValueKind != JsonValueKind.Object)
                            return (false, $"{par.Name} parameter need a object value");
                        if (type == "array" && p.ValueKind != JsonValueKind.Array)
                            return (false, $"{par.Name} parameter need a array value");
                        // todo: suppport securestring and secureObject
                        #endregion

                        var v = func.GetParameter(par.Name, context).Result;

                        if (par.Value.TryGetProperty("allowedValues", out JsonElement allowedValues) &&
                            !new JsonValue(allowedValues.GetRawText()).Contains(v))
                            return (false, $"the value of parameter {par.Name} is not in the scope of allowedValues");
                        if (type == "int" && par.Value.TryGetProperty("minValue", out JsonElement minValue) &&
                            (int)v < minValue.GetInt32())
                            return (false, $"the value of paratmter {par.Name} must greater than {minValue.GetInt32()}");
                        if (type == "int" && par.Value.TryGetProperty("maxValue", out JsonElement maxValue) &&
                            (int)v > maxValue.GetInt32())
                            return (false, $"the value of paratmter {par.Name} must less than {maxValue.GetInt32()}");
                        if (par.Value.TryGetProperty("minLength", out JsonElement minLength))
                        {
                            if (type == "string" && v.ToString().Length < minLength.GetInt32())
                                return (false, $"the value length of parameter {par.Name} must greater than {minLength.GetInt32()}");
                            if (type == "array" && (v as JsonValue).Length < minLength.GetInt32())
                                return (false, $"the arraty lenth of paramter {par.Name} must greater then {minLength.GetInt32()}");
                        }
                        if (par.Value.TryGetProperty("maxLength", out JsonElement maxLength))
                        {
                            if (type == "string" && v.ToString().Length > maxLength.GetInt32())
                                return (false, $"the value length of parameter {par.Name} must less than {maxLength.GetInt32()}");
                            if (type == "array" && (v as JsonValue).Length > maxLength.GetInt32())
                                return (false, $"the arraty lenth of paramter {par.Name} must less then {maxLength.GetInt32()}");
                        }
                    }
                }
            }

            return this.Template.Validate();
        }

        public static Deployment Parse(Resource resource)
        {
            Deployment deploymentContext = resource.Input;
            ARMFunctions functions = resource.ServiceProvider.GetService<ARMFunctions>();
            IInfrastructure infrastructure = resource.ServiceProvider.GetService<IInfrastructure>();
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
            string template = null;
            if (rootElement.TryGetProperty("template", out JsonElement _template))
            {
                template = _template.GetRawText();
            }
            TemplateLink templateLink = null;
            if (rootElement.TryGetProperty("templateLink", out JsonElement _templateLink))
                templateLink = resource.ServiceProvider.GetService<ARMTemplateHelper>().ParseTemplateLink(_templateLink, context);
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#scope-for-expressions-in-nested-templates
            string parameters = string.Empty;
            ParametersLink parametersLink = null;
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
            string expressionEvaluationOptions = "outer";
            if (rootElement.TryGetProperty("expressionEvaluationOptions", out JsonElement _expressionEvaluationOptions))
            {
                if (_expressionEvaluationOptions.TryGetProperty("scope", out JsonElement _scope))
                    expressionEvaluationOptions = functions.Evaluate(_scope.GetString(), context).ToString();
            }
            var (groupId, groupType, hierarchyId) = infrastructure.GetGroupInfo(resource.ManagementGroupId, resource.SubscriptionId, resource.ResourceGroup);
            context.Remove(ContextKeys.ARM_CONTEXT);
            var deployInput = new Deployment()
            {
                RootId = deploymentContext.RootId,
                DeploymentId = Guid.NewGuid().ToString("N"),
                ParentId = deploymentContext.ResourceId,
                _Parent = deploymentContext,
                GroupId = groupId,
                GroupType = groupType,
                HierarchyId = hierarchyId,
                CorrelationId = deploymentContext.CorrelationId,
                SubscriptionId = resource.SubscriptionId,
                ManagementGroupId = resource.ManagementGroupId,
                ResourceGroup = resource.ResourceGroup,
                Name = resource.Name,
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
                ServiceProvider = resource.ServiceProvider,
                ExpressionEvaluationOptions = expressionEvaluationOptions
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
        public string ExpressionEvaluationOptions { get; set; }
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
        private Deployment _Parent;
        [JsonIgnore]
        public Deployment Parent
        {
            get
            {
                if (string.IsNullOrEmpty(ParentId))
                    return null;
                if (_Parent == null)
                    _Parent = this.ServiceProvider.GetService<ARMTemplateHelper>().GetDeploymentByResourceId(this.ParentId);
                _Parent.IsRuntime = IsRuntime;
                return _Parent;
            }
        }

        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string Name { get; set; }

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
                if (template != null)
                    return template;
                if (TemplateLink != null)
                {
                    template = this.ServiceProvider.GetService<IInfrastructure>().GetTemplateContentAsync(TemplateLink, this).Result;
                    if (template != null)
                        template.Input = this;
                }
                return template;
            }
            set
            {
                template = value;
                if (template != null)
                    template.Input = this;
            }
        }
        string _Parameters = null;
        public string Parameters
        {
            get
            {
                if (!string.IsNullOrEmpty(_Parameters))
                    return _Parameters;
                if (ParametersLink != null)
                    return this.ServiceProvider.GetService<IInfrastructure>().GetParameterContentAsync(ParametersLink, this).Result;
                return null;
            }
            set
            {
                _Parameters = value;
            }
        }
        public string ApiVersion { get; set; }

        // todo: support OnErrorDeployment
        /// <summary>
        /// When a deployment fails, you can automatically redeploy an earlier, successful deployment from your deployment history. This functionality is useful if you've got a known good state for your infrastructure deployment and want to revert to this state.
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error#rest-api"/>
        public OnErrorDeployment OnErrorDeployment { get; set; }

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


        public IEnumerable<Deployment> EnumerateDeployments()
        {
            return Template.Resources.EnumerateDeployments();
        }
        public Resource GetFirstResource(string name, bool includeNestDeployment = false)
        {
            bool withServiceType = name.Contains('.');
            foreach (var r in Template.Resources)
            {
                if (r.Copy != null)
                {
                    if (withServiceType) { if (r.Copy.Id.EndsWith(name)) return r; }
                    else if (r.Copy.Name.EndsWith(name)) return r;

                    foreach (var item in r.Copy.EnumerateResource())
                    {
                        if (withServiceType) { if (item.ResourceId.EndsWith(name)) return r; }
                        else if (item.FullName.EndsWith(name)) return r;
                    }
                }
                else
                {
                    if (withServiceType) { if (r.ResourceId.EndsWith(name)) return r; }
                    else if (r.FullName.EndsWith(name)) return r;
                }
                foreach (var child in r.FlatEnumerateChild())
                {
                    if (withServiceType) { if (child.ResourceId.EndsWith(name)) return r; }
                    else if (child.FullName.EndsWith(name)) return r;
                }
            }
            if (includeNestDeployment)
            {
                foreach (var deploy in this.EnumerateDeployments())
                {
                    var dr = deploy.GetFirstResource(name, includeNestDeployment);
                    if (dr != null)
                        return dr;
                }
            }
            return null;
        }
        public List<Resource> GetResources(string name, bool includeNestDeployment = false)
        {
            List<Resource> resources = new List<Resource>();
            bool withServiceType = name.Contains('.');
            foreach (var r in Template.Resources)
            {
                if (r.Copy != null)
                {
                    if (withServiceType) { if (r.Copy.Id.EndsWith(name)) resources.Add(r); }
                    else if (r.Copy.Name.EndsWith(name)) resources.Add(r);

                    foreach (var item in r.Copy.EnumerateResource())
                    {
                        if (withServiceType) { if (item.ResourceId.EndsWith(name)) resources.Add(r); }
                        else if (item.FullName.EndsWith(name)) resources.Add(r);
                    }
                }
                else
                {
                    if (withServiceType) { if (r.ResourceId.EndsWith(name)) resources.Add(r); }
                    else if (r.FullName.EndsWith(name)) resources.Add(r);
                }
                foreach (var child in r.FlatEnumerateChild())
                {
                    if (withServiceType) { if (child.ResourceId.EndsWith(name)) resources.Add(child); }
                    else if (child.FullName.EndsWith(name)) resources.Add(child);
                }
            }
            if (includeNestDeployment)
            {
                foreach (var deploy in this.EnumerateDeployments())
                {
                    resources.AddRange(deploy.GetResources(name, true));
                }
            }
            return resources;
        }
        /// <summary>
        /// all resource in this Deployment
        /// </summary>
        /// <param name="flatChild">
        /// true: the result will include the child resource
        /// false: the result will only include the parent resource
        /// </param>
        /// <param name="expandCopy">
        /// true: the result will include all copied resources except the copy 
        /// false: the result will only include the copy resource
        /// </param>
        /// <param name="flatDeployment">
        /// true: the result will include all nest deployment‘resources  except the deployment 
        /// false: the result will only include the deployment
        /// </param>
        /// <returns></returns>
        public IEnumerable<Resource> EnumerateResource(bool flatChild = false, bool expandCopy = false, bool flatDeployment = false)
        {
            var infra = ServiceProvider.GetService<IInfrastructure>();
            foreach (var r in this.Template.Resources)
            {
                if (flatDeployment && r.Type == infra.BuiltinServiceTypes.Deployments)
                    continue;
                if (expandCopy && r.Copy != null)
                {
                    if (flatDeployment && r.Type == infra.BuiltinServiceTypes.Deployments)
                    {
                        foreach (var c in r.Copy.EnumerateResource(true))
                        {
                            var deploy = Deployment.Parse(c);
                            foreach (var rInDeploy in deploy.EnumerateResource(flatChild, expandCopy, flatDeployment))
                            {
                                yield return rInDeploy;
                            }
                        }
                    }
                    else
                    {
                        foreach (var c in r.Copy.EnumerateResource(true))
                        {
                            yield return c;
                        }
                    }

                    continue;
                }
                yield return r;
                if (flatChild)
                {
                    foreach (var child in r.FlatEnumerateChild())
                    {
                        yield return child;
                    }
                }
            }
            if (flatDeployment)
            {
                foreach (var d in this.Template.Resources.EnumerateDeployments())
                {
                    foreach (var r in d.EnumerateResource())
                    {
                        yield return r;
                        if (flatChild)
                        {
                            foreach (var child in r.FlatEnumerateChild())
                            {
                                yield return child;
                            }
                        }
                    }
                }
            }

        }
        [JsonIgnore]
        public string ResourceId
        {
            get
            {
                var infrastructure = ServiceProvider.GetService<IInfrastructure>();
                string resourceId = string.Empty;
                if (!string.IsNullOrEmpty(this.SubscriptionId))
                    resourceId = $"/{infrastructure.BuiltinPathSegment.Subscription}/{this.SubscriptionId}";
                if (!string.IsNullOrEmpty(this.ManagementGroupId))
                    resourceId = $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{this.ManagementGroupId}";
                if (!string.IsNullOrEmpty(this.ResourceGroup))
                    resourceId += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{this.ResourceGroup}";
                resourceId += $"/{infrastructure.BuiltinPathSegment.Provider}/{infrastructure.BuiltinServiceTypes.Deployments}/{this.Name}";
                return resourceId;
            }
        }
        public Dictionary<string, object> Context = new Dictionary<string, object>();

        public string GetOutputs()
        {
            // https://docs.microsoft.com/en-us/rest/api/resources/deployments/get#deploymentextended
            var infrastructure = ServiceProvider.GetService<IInfrastructure>();
            var aRMFunctions = ServiceProvider.GetService<ARMFunctions>();
            Dictionary<string, object> context = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, this } };
            var outputDefineElement = this.Template.Outputs.RootElement;
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions() { Indented = false });
            writer.WriteStartObject();
            writer.WriteString("id", this.DeploymentId);
            // TODO: set location
            writer.WriteString("location", this.ResourceGroup);
            writer.WriteString("name", this.Name);
            writer.WriteString("type", infrastructure.BuiltinServiceTypes.Deployments);

            #region properties

            writer.WritePropertyName("properties");
            writer.WriteStartObject();

            #region outputs

            writer.WritePropertyName("outputs");
            writer.WriteStartObject();
            foreach (var item in outputDefineElement.EnumerateObject())
            {
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-outputs?tabs=azure-powershell#conditional-output
                if (item.Value.TryGetProperty("condition", out JsonElement condition))
                {
                    if (condition.ValueKind == JsonValueKind.False)
                        continue;
                    if (condition.ValueKind == JsonValueKind.String &&
                        !(bool)aRMFunctions.Evaluate(condition.GetString(), context))
                        continue;
                }
                writer.WriteProperty(item, context, aRMFunctions, infrastructure);
            }
            writer.WriteEndObject();

            #endregion outputs

            writer.WriteEndObject();

            #endregion properties

            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}