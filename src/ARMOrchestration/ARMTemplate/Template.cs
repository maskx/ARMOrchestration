using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Template : ChangeTracking
    {
        private Dictionary<string, object> _FullContext;

        internal Dictionary<string, object> FullContext
        {
            get
            {
                if (_FullContext == null)
                {
                    _FullContext = new Dictionary<string, object> {
                        {ContextKeys.ARM_CONTEXT,this.Input} };
                    foreach (var item in Input.Context)
                    {
                        if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                        _FullContext.Add(item.Key, item.Value);
                    }
                }
                return _FullContext;
            }
        }

        public DeploymentOrchestrationInput Input { get; set; }

        private IServiceProvider ServiceProvider { get { return Input.ServiceProvider; } }

        public static implicit operator Template(string rawString)
        {
            return new Template() { RawString = rawString };
        }

        [DisplayName("$schema")]
        public string Schema
        {
            get
            {
                if (!RootElement.TryGetProperty("$schema", out JsonElement schema))
                    throw new Exception("not find $schema in template");
                return schema.GetString();
            }
        }

        [DisplayName("contentVersion")]
        public string ContentVersion
        {
            get
            {
                if (!RootElement.TryGetProperty("contentVersion", out JsonElement contentVersion))
                    throw new Exception("not find contentVersion in template");
                return contentVersion.GetString();
            }
        }

        [DisplayName("apiProfile")]
        public string ApiProfile
        {
            get
            {
                if (RootElement.TryGetProperty("apiProfile", out JsonElement apiProfile))
                    return this.ServiceProvider.GetService<ARMFunctions>().Evaluate(
                        apiProfile.GetString(),
                        FullContext
                        ).ToString();
                return string.Empty;
            }
        }

        [DisplayName("parameters")]
        public string Parameters
        {
            get
            {
                if (RootElement.TryGetProperty("parameters", out JsonElement parameters))
                    return parameters.GetRawText();
                return string.Empty;
            }
        }

        public string _Variables = null;

        [DisplayName("variables")]
        public string Variables
        {
            get
            {
                if (_Variables == null)
                {
                    _Variables = string.Empty;
                    if (RootElement.TryGetProperty("variables", out JsonElement variables))
                    {
                        // variable can refernce variable, so must set variables value before expand
                        _Variables = variables.GetRawText();
                        _Variables = variables.ExpandObject(new Dictionary<string, object>() {
                            { ContextKeys.ARM_CONTEXT,Input} },
                            ServiceProvider.GetService<ARMFunctions>(),
                            ServiceProvider.GetService<IInfrastructure>());
                    }
                }
                return _Variables;
            }
        }

        //private void ExpandResource(JsonElement element, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        //{
        //    DeploymentOrchestrationInput input = fullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
        //    foreach (var resource in element.EnumerateArray())
        //    {
        //        var r = new Resource(resource, fullContext, parentName, parentType);
        //        _Resources.Add(r);
        //        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
        //        // You can't use a copy loop for a child resource.
        //        if (resource.TryGetProperty("resources", out JsonElement _resources))
        //        {
        //            ExpandResource(_resources, r.FullContext, r.FullName, r.FullType);
        //        }
        //    }
        //}

        private ResourceCollection _Resources;

        [DisplayName("resources")]
        public ResourceCollection Resources
        {
            get
            {
                if (_Resources == null)
                {
                    if (!RootElement.TryGetProperty("resources", out JsonElement resources))
                        throw new Exception("not find resources in template");
                    _Resources = new ResourceCollection(resources, this.FullContext);
                }
                return _Resources;
            }
        }

        [DisplayName("functions")]
        public Functions Functions
        {
            get
            {
                if (RootElement.TryGetProperty("functions", out JsonElement funcs))
                    return Functions.Parse(funcs);
                return null;
            }
        }

        private ChangeTracking _Outputs;

        [DisplayName("outputs")]
        public ChangeTracking Outputs
        {
            get
            {
                if (_Outputs == null)
                {
                    if (RootElement.TryGetProperty("outputs", out JsonElement outputs))
                        _Outputs = new ChangeTracking() { RawString = outputs.GetRawText() };
                    else
                        _Outputs = new ChangeTracking();
                }
                return _Outputs;
            }
            set
            {
                _Outputs = value;
                Change(value, "outputs");
            }
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-subscription
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-management-group
        /// </summary>
        public DeployLevel DeployLevel
        {
            get
            {
                if (this.Schema.EndsWith("deploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                    return DeployLevel.ResourceGroup;
                if (this.Schema.EndsWith("subscriptionDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                    return DeployLevel.Subscription;
                if (this.Schema.EndsWith("managementGroupDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                    return DeployLevel.ManagemnetGroup;
                throw new Exception("wrong $shema setting");
            }
        }

        internal (bool, string) Validate()
        {
            object _;
            try
            {
                if (!RootElement.TryGetProperty("$schema", out JsonElement schema))
                    return (false, "not find $schema in template");
                if (!RootElement.TryGetProperty("contentVersion", out JsonElement contentVersion))
                    return (false, "not find contentVersion in template");
                _ = this.ApiProfile;
                _ = this.Functions;
                foreach (var res in this.Resources)
                {
                    var (r, m) = res.Validate();
                    if (!r) return (r, m);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (true, "");
        }
    }
}