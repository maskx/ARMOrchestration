using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{

    public class Template : ChangeTracking
    {
        [JsonProperty]
        private string RawString;
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
        internal JObject RootElement
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

        public string Schema
        {
            get
            {
                if (!RootElement.TryGetValue("$schema", out JToken schema))
                    throw new Exception("not find $schema in template");
                return schema.Value<string>();
            }
        }

        public string ContentVersion
        {
            get
            {
                if (!RootElement.TryGetValue("contentVersion", out JToken contentVersion))
                    throw new Exception("not find contentVersion in template");
                return contentVersion.ToString();
            }
        }

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

        public string Outputs
        {
            get
            {
                if (RootElement.TryGetValue("outputs", out JToken outputs))
                    return outputs.ToString();

                return null;
            }
            set
            {
                // todo: template.Outputs
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


        public override string ToString()
        {
            return RootElement.ToString(Formatting.Indented);
        }
    }
}