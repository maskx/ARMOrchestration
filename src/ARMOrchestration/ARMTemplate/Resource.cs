using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    return _Functions.Evaluate(kind.GetString(), FullContext).ToString();
                return string.Empty;
            }
        }

        public Plan Plan
        {
            get
            {
                if (RootElement.TryGetProperty("plan", out JsonElement planE))
                    return new Plan(planE, FullContext);
                return null;
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
            return this.RootElement.GetRawText();
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
        }

        internal (bool, string) Validate()
        {
            try
            {
                if (!RootElement.TryGetProperty("type", out JsonElement type))
                    return (false, "not find type in resource node");
                if (this.Copy != null && !this.CopyIndex.HasValue)
                {
                    // using first resource validate copy resource content
                    var r = new Resource()
                    {
                        RawString = this.RawString,
                        CopyIndex = 0,
                        ParentContext = ParentContext,
                        Input = Input
                    };
                    return r.Validate();
                }
                else
                {
                    object _;
                    _ = this.Name;
                    _ = this.ApiVersion;
                    _ = this.Condition;
                    _ = this.Location;
                    _ = this.SKU;
                    _ = this.Kind;
                    if (this.Plan != null)
                    {
                        var (pv, pm) = this.Plan.Validate();
                        if (!pv) return (pv, pm);
                    }
                    // validate properties and dependson and parameter and variables
                    LazyLoadDependsOnAnProperties();
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (true, string.Empty);
        }
    }
}