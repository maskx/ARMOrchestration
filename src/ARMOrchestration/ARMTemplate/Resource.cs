using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Resource : IDisposable
    {
        [JsonProperty]
        public string RawString
        {
            get { return RootElement.GetRawText(); }
            set { json = JsonDocument.Parse(value); }
        }

        [JsonProperty]
        protected readonly string _ParentName;

        [JsonProperty]
        protected readonly string _ParentType;

        /// <summary>
        /// if CopyIndex has value, means this is a expanded resource
        /// </summary>
        [JsonProperty]
        public int? CopyIndex { get; set; }

        /// <summary>
        /// for nesting copy, this context should save all copy context from root to direct parent
        /// for clear serialize, this context not include ContextKeys.ARM_CONTEXT
        /// at runtime, should set ContextKeys.ARM_CONTEXT after deserialize
        /// </summary>
        [JsonProperty]
        public Dictionary<string, object> ParentContext;

        private Dictionary<string, object> _FullContext;

        internal Dictionary<string, object> FullContext
        {
            get
            {
                if (_FullContext == null)
                {
                    _FullContext = new Dictionary<string, object> {
                        {ContextKeys.ARM_CONTEXT,this.Input} };
                    if (ParentContext != null)
                    {
                        foreach (var item in ParentContext)
                        {
                            _FullContext.Add(item.Key, item.Value);
                        }
                    }
                    if (Copy != null && CopyIndex.HasValue)
                    {
                        Dictionary<string, int> indexDic = new Dictionary<string, int>();
                        if (_FullContext.TryGetValue(ContextKeys.COPY_INDEX, out object indexOjb))
                        {
                            foreach (var item in indexOjb as Dictionary<string, int>)
                            {
                                indexDic[item.Key] = item.Value;
                            }
                        }
                        indexDic[Copy.Name] = this.CopyIndex.Value;
                        _FullContext[ContextKeys.COPY_INDEX] = indexDic;
                        _FullContext[ContextKeys.CURRENT_LOOP_NAME] = Copy.Name;
                    }
                }
                return _FullContext;
            }
        }

        public DeploymentOrchestrationInput Input { get; set; }
        protected readonly JsonElement? _Element;
        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        internal IServiceProvider ServiceProvider { get { return Input.ServiceProvider; } }

        private JsonDocument json = null;

        internal JsonElement RootElement
        {
            get
            {
                if (!_Element.HasValue)
                {
                    if (json == null)
                        json = JsonDocument.Parse(RawString);
                    return json.RootElement;
                }
                return _Element.Value;
            }
        }

        public Resource()
        {
        }

        public Resource(JsonElement element, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        {
            DeploymentOrchestrationInput input = fullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
            this._Element = element;
            this._ParentName = parentName;
            this._ParentType = parentType;
            this.Input = input;
            this.ParentContext = new Dictionary<string, object>();
            foreach (var item in fullContext)
            {
                if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                ParentContext.Add(item.Key, item.Value);
            }
        }

        public bool Condition
        {
            get
            {
                if (RootElement.TryGetProperty("condition", out JsonElement condition))
                {
                    if (condition.ValueKind == JsonValueKind.False)
                        return false;
                    if (condition.ValueKind == JsonValueKind.True)
                        return true;
                    if (condition.ValueKind == JsonValueKind.String)
                        return (bool)this._Functions.Evaluate(condition.GetString(), FullContext);
                    return true;
                }
                return true;
            }
        }

        public string ApiVersion
        {
            get
            {
                if (RootElement.TryGetProperty("apiVersion", out JsonElement apiVersion))
                    return apiVersion.GetString();
                throw new Exception("not find apiVersion in resource node");
            }
        }

        /// <summary>
        /// the type set in template
        /// </summary>
        public string Type
        {
            get
            {
                if (RootElement.TryGetProperty("type", out JsonElement type))
                    return type.GetString();
                throw new Exception("not find type in resource node");
            }
        }

        /// <summary>
        /// the type of resource
        /// the child resource is parentType/childType
        /// </summary>
        public string FullType
        {
            get
            {
                if (string.IsNullOrEmpty(this._ParentType))
                    return this.Type;
                return $"{this._ParentType}/{this.Type}";
            }
        }

        /// <summary>
        /// the name set in template
        /// </summary>
        public string Name
        {
            get
            {
                if (Copy != null && !CopyIndex.HasValue)
                    return Copy.Name;
                if (RootElement.TryGetProperty("name", out JsonElement name))
                    return this._Functions.Evaluate(name.GetString(), FullContext).ToString();
                throw new Exception("not find name in resource node");
            }
        }

        /// <summary>
        /// the name of resource
        /// the child resource is parentName/childName
        /// </summary>
        public string FullName
        {
            get
            {
                if (Copy != null && !CopyIndex.HasValue)
                    return Copy.FullName;
                if (string.IsNullOrEmpty(_ParentName))
                    return this.Name;
                return $"{_ParentName}/{this.Name}";
            }
        }

        public string Location
        {
            get
            {
                if (RootElement.TryGetProperty("location", out JsonElement location))
                    return this._Functions.Evaluate(location.GetString(), FullContext).ToString();
                return string.Empty;
            }
        }

        public string Comments
        {
            get
            {
                if (RootElement.TryGetProperty("comments", out JsonElement comments))
                    return comments.GetString();
                return string.Empty;
            }
        }

        private DependsOnCollection _DependsOn;

        /// <summary>
        /// The list can include resources that are conditionally deployed. When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
        ///
        /// Depend on resources in a loop
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#depend-on-resources-in-a-loop
        ///
        ///
        /// You only need to define dependencies for resources that are deployed in the same template.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency
        ///
        /// </summary>
        public DependsOnCollection DependsOn
        {
            get
            {
                LazyLoadDependsOnAnProperties();
                return _DependsOn;
            }
        }

        private string _Properties;

        public string Properties
        {
            get
            {
                LazyLoadDependsOnAnProperties();
                return _Properties;
            }
        }

        private void LazyLoadDependsOnAnProperties()
        {
            if (_DependsOn != null)
                return;
            _DependsOn = new DependsOnCollection();
            if (RootElement.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    _DependsOn.Add(_Functions.Evaluate(item.GetString(), FullContext).ToString(), Input.Template.Resources);
                }
            }
            if (RootElement.TryGetProperty("properties", out JsonElement properties))
            {
                var infrastructure = ServiceProvider.GetService<IInfrastructure>();
                if (this.FullType == infrastructure.BuiltinServiceTypes.Deployments)
                    _Properties = properties.GetRawText();
                else
                {
                    _Properties = properties.ExpandObject(FullContext, _Functions, infrastructure);
                    // if there has Implicit dependency by reference function in properties
                    // the reference function should be evaluate at provisioning time
                    // so keep the original text
                    if (FullContext.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
                    {
                        _DependsOn.AddRange(conditionDep as List<string>, Input.Template.Resources);
                        FullContext.Remove(ContextKeys.DEPENDSON);
                    }
                }
            }
        }

        public SKU SKU
        {
            get
            {
                if (RootElement.TryGetProperty("sku", out JsonElement sku))
                    return SKU.Parse(sku, _Functions, FullContext);
                return new SKU() { Name = SKU.Default };
            }
        }

        public string Kind
        {
            get
            {
                if (RootElement.TryGetProperty("kind", out JsonElement kind))
                    return kind.GetString();
                return string.Empty;
            }
        }

        public string Plan
        {
            get
            {
                if (RootElement.TryGetProperty("plan", out JsonElement plan))
                    return plan.GetRawText();
                return string.Empty;
            }
        }

        private List<string> _Zones;

        public List<string> Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = new List<string>();
                    if (RootElement.TryGetProperty("zones", out JsonElement zones))
                    {
                        if (zones.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var z in zones.EnumerateArray())
                            {
                                _Zones.Add(_Functions.Evaluate(z.GetString(), FullContext).ToString());
                            }
                        }
                        else if (zones.ValueKind == JsonValueKind.String)
                        {
                            var arr = _Functions.Evaluate(zones.GetString(), FullContext) as JsonValue;
                            if (arr == null || arr.ValueKind != JsonValueKind.Array)
                                throw new Exception("wrong value of zones");
                            for (int i = 0; i < arr.Length; i++)
                            {
                                _Zones.Add(arr[i].ToString());
                            }
                        }
                    }
                }

                return _Zones;
            }
        }

        public string ResourceGroup
        {
            get
            {
                if (RootElement.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                    return _Functions.Evaluate(resourceGroup.GetString(), FullContext).ToString();
                else
                    return (FullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput).ResourceGroup;
            }
        }

        public string SubscriptionId
        {
            get
            {
                if (RootElement.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                    return _Functions.Evaluate(subscriptionId.GetString(), FullContext).ToString();
                else
                    return (FullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput).SubscriptionId;
            }
        }

        public string ManagementGroupId
        {
            get
            {
                if (RootElement.TryGetProperty("managementGroupId", out JsonElement managementGroupId))
                    return _Functions.Evaluate(managementGroupId.GetString(), FullContext).ToString();
                else
                    return (FullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput).ManagementGroupId;
            }
        }

        public string ResourceId
        {
            get
            {
                if (Copy != null && !CopyIndex.HasValue)
                    return Copy.Id;
                if (Input.Template.DeployLevel == DeployLevel.ResourceGroup)
                {
                    List<object> pars = new List<object>{
                        SubscriptionId,
                        ResourceGroup,
                        FullType};
                    pars.AddRange(FullName.Split('/'));
                    return _Functions.ResourceId(Input, pars.ToArray());
                }
                if (Input.Template.DeployLevel == DeployLevel.Subscription)
                {
                    List<object> pars = new List<object> { SubscriptionId, FullType };
                    pars.AddRange(FullName.Split('/'));
                    return _Functions.SubscriptionResourceId(Input, pars.ToArray());
                }
                List<object> pars1 = new List<object> { FullType };
                pars1.AddRange(FullName.Split('/'));
                return _Functions.TenantResourceId(pars1.ToArray());
            }
        }

        public string CopyId { get; set; }

        public string CopyName { get; set; }

        public Copy Copy
        {
            get
            {
                if (!RootElement.TryGetProperty("copy", out JsonElement copy))
                    return null;
                var cxt = new Dictionary<string, object> {
                        {ContextKeys.ARM_CONTEXT,this.Input} };
                if (ParentContext != null)
                {
                    foreach (var item in ParentContext)
                    {
                        cxt.Add(item.Key, item.Value);
                    }
                }
                return Copy.Parse(copy, cxt, _Functions, ServiceProvider.GetService<IInfrastructure>());
            }
        }

        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteBoolean("condition", this.Condition);
            writer.WriteString("apiVersion", this.ApiVersion);
            writer.WriteString("type", this.FullType);
            writer.WriteString("name", this.FullName);
            if (!string.IsNullOrEmpty(this.Location))
                writer.WriteString("location", this.Location);
            if (!string.IsNullOrEmpty(this.Comments))
                writer.WriteString("comments", this.Comments);
            if (this.DependsOn.Count > 0)
            {
                writer.WritePropertyName("dependsOn");
                writer.WriteStartArray();
                foreach (var depend in this.DependsOn)
                {
                    writer.WriteStringValue(depend);
                }
                writer.WriteEndArray();
            }

            if (!string.IsNullOrEmpty(this.Properties))
                writer.WriteRawString("properties", this.Properties);
            if (this.SKU != null)
                writer.WriteRawString("sku", this.SKU.ToString());
            if (!string.IsNullOrEmpty(this.Kind))
                writer.WriteString("kind", this.Kind);
            if (!string.IsNullOrEmpty(this.Plan))
                writer.WriteRawString("plan", this.Plan);
            if (!string.IsNullOrEmpty(this.ResourceGroup))
                writer.WriteString("resourceGroup", this.ResourceGroup);
            if (!string.IsNullOrEmpty(this.SubscriptionId))
                writer.WriteString("subscriptionId", this.SubscriptionId);
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static List<Resource> Parse(
            string rawString
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            using var doc = JsonDocument.Parse(rawString);
            return Parse(doc.RootElement, context, functions, infrastructure, parentName, parentType);
        }

        private static List<Resource> ParseInternal(
            JsonElement resourceElement
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            var deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
            List<Resource> resources = new List<Resource>();
            Resource r = null;
            resources.Add(r);

            if (context.ContainsKey(ContextKeys.DEPENDSON))
            {
                //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                throw new Exception("The reference function can only be used in the properties of a resource definition and the outputs section of a template or deployment.");
            }

            if (!r.Condition)
                return resources;

            if (resourceElement.TryGetProperty("resources", out JsonElement _resources))
            {
                foreach (var childres in _resources.EnumerateArray())
                {
                    //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
                    var childResult = Resource.Parse(childres.GetRawText(), context, functions, infrastructure, r.Name, r.Type);
                    //  r.Resources.Add(childResult[0].Name);
                    resources.AddRange(childResult);
                }
            }
            return resources;
        }

        public static List<Resource> Parse(
            JsonElement resourceElement
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            if (resourceElement.TryGetProperty("copy", out JsonElement copy))
                return ExpandCopyResource(resourceElement, copy, context, functions, infrastructure, parentName, parentType);
            return ParseInternal(resourceElement, context, functions, infrastructure, parentName, parentType);
        }

        private static List<Resource> ExpandCopyResource(
          JsonElement resource
          , JsonElement copyElement
          , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName
            , string parentType)
        {
            Copy copy = Copy.Parse(copyElement, context, functions, infrastructure);

            var deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;

            CopyResource CopyResource = null;
            //new CopyResource()
            //{
            //    //Name = copy.Name,
            //    //Type = Copy.ServiceType,
            //    //FullName = $"{deploymentContext.DeploymentName}/{copy.Name}",
            //    //FullType = $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}",
            //    //ResourceId = copy.Id,
            //    Mode = copy.Mode,
            //    BatchSize = copy.BatchSize,
            //};
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
                var rs = ParseInternal(resource, copyContext, functions, infrastructure, parentName, parentType);

                rs[0].CopyIndex = i;
                rs[0].CopyId = copy.Id;
                rs[0].CopyName = copy.Name;
                //  CopyResource.Resources.Add(rs[0].Name);
                resources.AddRange(rs);
            }
            //CopyResource.SubscriptionId = resources[1].SubscriptionId;
            //CopyResource.ManagementGroupId = resources[1].ManagementGroupId;
            //CopyResource.SKU = resources[1].SKU;
            //CopyResource.Plan = resources[1].Plan;
            //CopyResource.Kind = resources[1].Kind;
            //CopyResource.Zones = resources[1].Zones;
            //CopyResource.Location = resources[1].Location;
            return resources;
        }

        public string ExpandProperties(DeploymentOrchestrationInput deploymentContext, ARMFunctions functions, IInfrastructure infrastructure)
        {
            if (string.IsNullOrEmpty(this.Properties))
                return string.Empty;
            {
                var doc = JsonDocument.Parse(this.Properties);
                Dictionary<string, object> cxt = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, deploymentContext } };
                if (!string.IsNullOrEmpty(this.CopyName))
                {
                    cxt.Add(ContextKeys.CURRENT_LOOP_NAME, this.CopyName);
                    cxt.Add(ContextKeys.COPY_INDEX, new Dictionary<string, int>() { { this.CopyName, this.CopyIndex.Value } });
                }
                return doc.RootElement.ExpandObject(cxt, functions, infrastructure);
            }
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
        }
    }
}