using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{

    public class Template : ChangeTracking
    {
        [JsonProperty]
        public string RawString { get; private set; }
        private TemplateLink _TemplateLink { get; set; }
        public TemplateLink TemplateLink
        {
            get { return _TemplateLink; }
            set
            {
                _TemplateLink = value;
                _RootElement = null;
            }
        }
        JObject _RootElement;
        internal  JObject RootElement
        {
            get
            {
                if (_RootElement == null)
                {
                    if (_TemplateLink != null)
                        _RootElement = JObject.Parse(this.ServiceProvider.GetService<IInfrastructure>().GetTemplateContentAsync(_TemplateLink, Deployment).Result);
                    else if (!string.IsNullOrEmpty(this.RawString))
                        _RootElement = JObject.Parse(this.RawString);

                }
                return _RootElement;
            }
        }
        public static implicit operator Template(string rawString)
        {
            if (string.IsNullOrEmpty(rawString))
                return null;
            return new Template() { RawString = rawString };
        }

        [DisplayName("$schema")]
        public string Schema
        {
            get
            {
                if (!RootElement.TryGetValue("$schema", out JToken schema))
                    throw new Exception("not find $schema in template");
                return schema.Value<string>();
            }
        }

        [DisplayName("contentVersion")]
        public string ContentVersion
        {
            get
            {
                if (!RootElement.TryGetValue("contentVersion", out JToken contentVersion))
                    throw new Exception("not find contentVersion in template");
                return contentVersion.ToString();
            }
        }

        [DisplayName("apiProfile")]
        public string ApiProfile
        {
            get
            {
                if (RootElement.TryGetValue("apiProfile", out JToken apiProfile))
                    return this.ServiceProvider.GetService<ARMFunctions>().Evaluate(
                        apiProfile.Value<string>(),
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
                if (RootElement.TryGetValue("parameters", out JToken parameters))
                    return parameters.ToString();
                return "{}";
            }
        }

        public string _Variables = null;

        // thread unsafed
        // todo: keep newguid result
        [DisplayName("variables")]
        public string Variables
        {
            get
            {
                if (_Variables == null)
                {
                    if (RootElement.TryGetValue("variables", out JToken variables))
                    {
                        // variable can refernce variable, so must set variables value before expand
                        _Variables = variables.ToString();
                    }
                }
                return _Variables;
            }
        }

        private ResourceCollection _Resources;

        [DisplayName("resources")]
        public ResourceCollection Resources
        {
            get
            {
                if (_Resources == null)
                {
                    if (!RootElement.TryGetValue("resources", out JToken resources))
                        throw new Exception("not find resources in template");
                    _Resources = new ResourceCollection(resources as JArray, this.FullContext);
                }
                return _Resources;
            }
        }

        [DisplayName("functions")]
        public Functions Functions
        {
            get
            {
                if (RootElement.TryGetValue("functions", out JToken funcs))
                {
                    using var doc = JsonDocument.Parse(funcs.ToString());
                    return Functions.Parse(doc.RootElement);
                }
                return null;
            }
        }

        private string _Outputs;
        [JsonProperty]
        private string ChangedOutputs;

        [DisplayName("outputs")]
        public string Outputs
        {
            get
            {
                if (_Outputs == null)
                {
                    if (RootElement.TryGetValue("outputs", out JToken outputs))
                        _Outputs = outputs.ToString();
                }
                return string.IsNullOrEmpty(ChangedOutputs) ? _Outputs : ChangedOutputs;
            }
            set
            {
                ChangedOutputs = value;
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
                if (!RootElement.TryGetValue("$schema", out JToken schema))
                    return (false, "not find $schema in template");
                if (!RootElement.TryGetValue("contentVersion", out JToken contentVersion))
                    return (false, "not find contentVersion in template");
                _ = this.Variables;
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

        public override string ToString()
        {
            return RootElement.ToString(Formatting.Indented);
        }
    }
}