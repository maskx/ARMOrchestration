using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Template
    {
        [JsonProperty]
        private string _RawString { get; set; }

        public DeploymentOrchestrationInput Input { get; set; }

        private IServiceProvider ServiceProvider { get { return Input.ServiceProvider; } }

        private JsonDocument json = null;

        internal JsonElement RootElement
        {
            get
            {
                if (json == null)
                    json = JsonDocument.Parse(_RawString);
                return json.RootElement;
            }
        }

        public Template()
        {
            _RawString = "{}";
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
        }

        public static implicit operator Template(string rawString)
        {
            return new Template() { _RawString = rawString };
        }

        public string Schema
        {
            get
            {
                if (!RootElement.TryGetProperty("$schema", out JsonElement schema))
                    throw new Exception("not find $schema in template");
                return schema.GetString();
            }
        }

        public string ContentVersion
        {
            get
            {
                if (!RootElement.TryGetProperty("contentVersion", out JsonElement contentVersion))
                    throw new Exception("not find contentVersion in template");
                return contentVersion.GetString();
            }
        }

        public string ApiProfile
        {
            get
            {
                if (RootElement.TryGetProperty("apiProfile", out JsonElement apiProfile))
                    return apiProfile.GetString();
                return string.Empty;
            }
        }

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

        private void ExpandResource(JsonElement element, Dictionary<string, object> context, string parentName = null, string parentType = null)
        {
            DeploymentOrchestrationInput input = context[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
            foreach (var resource in element.EnumerateArray())
            {
                var r = new Resource(resource, input, parentName, parentType);
                _Resources.Add(r);
                if (resource.TryGetProperty("resources", out JsonElement _resources))
                {
                    ExpandResource(_resources, context, r.FullName, r.FullType);
                }
            }
        }

        private ResourceCollection _Resources;

        public ResourceCollection Resources
        {
            get
            {
                if (_Resources == null)
                {
                    if (!RootElement.TryGetProperty("resources", out JsonElement resources))
                        throw new Exception("not find resources in template");

                    _Resources = new ResourceCollection();
                    ExpandResource(resources, new Dictionary<string, object> {
                        { ContextKeys.ARM_CONTEXT, this.Input }
                    });
                }

                return _Resources;
            }
        }

        public Functions Functions
        {
            get
            {
                if (RootElement.TryGetProperty("functions", out JsonElement funcs))

                    return Functions.Parse(funcs);
                return null;
            }
        }

        public string Outputs
        {
            get
            {
                if (RootElement.TryGetProperty("outputs", out JsonElement outputs))
                    return outputs.GetRawText();
                return string.Empty;
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
            return this._RawString;
        }
    }
}