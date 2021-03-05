using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource : ObjectChangeTracking
    {
        #region 构造函数
        public Resource() : base(new JObject(), null)
        {
        }

        public Resource(JObject root, Dictionary<string, object> fullContext, int index) : base(root, fullContext)
        {
            this.CopyIndex = index;

        }
        public Resource(JObject root, Dictionary<string, object> fullContext, string parentName = null, string parentType = null) : base(root, fullContext)
        {
            this._ParentName = parentName;
            this._ParentType = parentType;
        }
        #endregion

        #region private member
        private bool? _Condition;
        private string _ApiVersion = null;
        private string _Type = null;
        private string _Name = null;
        private string _Location = null;
        private string _Comments = null;
        private DependsOnCollection _DependsOn;
        private string _Properties;
        private SKU _SKU;
        private string _Kind;
        private Plan _Plan = null;
        private List<string> _Zones;
        private ResourceCollection _Resources;
        private Copy _Copy = null;
        private string _resourceGroup = null;
        private string _SubscriptionId = null;
        public string _ManagementGroupId = null;
        #endregion

        #region Resource Property
        [JsonProperty]
        public bool Condition
        {
            get
            {
                if (!_Condition.HasValue)
                {
                    _Condition = true;
                    if (!string.IsNullOrEmpty(_ParentName))
                    {
                        var r = Deployment.GetFirstResource(_ParentName);
                        _Condition = r.Condition;
                    }
                    else if (RootElement.TryGetValue("condition", out JToken condition))
                    {
                        if (condition.Type == JTokenType.Boolean)
                            _Condition = condition.Value<bool>();
                        else if (condition.Type == JTokenType.String)
                            _Condition = (bool)this._Functions.Evaluate(condition.Value<string>(), FullContext, $"{RootElement.Path}.{condition}");
                        else
                            _Condition = true;
                    }

                }
                return _Condition.Value;
            }
            set
            {
                _Condition = value;
                RootElement["condition"] = value;
            }
        }
        [JsonProperty]
        public string ApiVersion
        {
            get
            {
                if (_ApiVersion == null)
                {
                    if (RootElement.TryGetValue("apiVersion", out JToken apiVersion))
                        _ApiVersion = apiVersion.Value<string>();
                    else
                        throw new Exception("not find apiVersion in resource node");
                }
                return _ApiVersion;
            }
            set
            {
                _ApiVersion = value;
                if (value == null)
                    RootElement.Remove("apiVersion");
                else
                    RootElement["apiVersion"] = value;
            }
        }
        [JsonProperty]
        public string Type
        {
            get
            {
                if (_Type == null)
                {
                    if (RootElement.TryGetValue("type", out JToken type))
                        _Type = type.Value<string>();
                    else
                        throw new Exception("not find type in resource node");
                }
                return _Type;
            }
            set
            {
                _Type = value;
                if (value == null)
                    RootElement.Remove("type");
                else
                    RootElement["type"] = value;
            }
        }
        [JsonProperty]
        public string Name
        {
            get
            {
                if (_Name == null)
                {
                    if (!RootElement.TryGetValue("name", out JToken name))
                        throw new Exception("not find name in resource node");
                    if (Copy != null && !CopyIndex.HasValue)
                        _Name = name.Value<string>();
                    else
                        _Name = this._Functions.Evaluate(name.Value<string>(), FullContext, $"{RootElement.Path}.name").ToString();
                }
                return _Name;
            }
            set
            {
                _Name = value;
                if (value == null)
                    RootElement.Remove("name");
                else
                    RootElement["name"] = value;
            }
        }
        [JsonProperty]
        public string Location
        {
            get
            {
                if (_Location == null)
                {
                    if (RootElement.TryGetValue("location", out JToken location))
                        _Location = this._Functions.Evaluate(location.Value<string>(), FullContext, $"{RootElement.Path}.location").ToString();
                    else
                        _Location = string.Empty;
                }
                return _Location;
            }
            set
            {
                _Location = value;
                if (value == null)
                    RootElement.Remove("location");
                else
                    RootElement["location"] = value;
            }
        }
        [JsonProperty]
        public string Comments
        {
            get
            {
                if (_Comments == null)
                {
                    if (RootElement.TryGetValue("comments", out JToken comments))
                        _Comments = comments.Value<string>();
                    else
                        _Comments = string.Empty;
                }
                return _Comments;
            }
            set
            {
                _Comments = value;
                if (value == null)
                    RootElement.Remove("comments");
                else
                    RootElement["comments"] = value;
            }
        }
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
        [JsonProperty]
        public DependsOnCollection DependsOn
        {
            get
            {
                if (_DependsOn == null || _PropertiesNeedReload)
                    LazyLoadDependsOnAnProperties();
                return _DependsOn;
            }
            set
            {
                _DependsOn = value;
                if (value == null)
                    RootElement.Remove("dependsOn");
                else
                    RootElement["dependsOn"] = JArray.Parse(value.ToString());
            }
        }
        [JsonProperty]
        public string Properties
        {
            get
            {
                if (string.IsNullOrEmpty(_Properties) || _PropertiesNeedReload)
                    LazyLoadDependsOnAnProperties();
                return _Properties;
            }
            set
            {
                _Properties = value;
                if (value == null)
                    RootElement.Remove("properites");
                else
                    RootElement["properites"] = JObject.Parse(value);
            }
        }
        [JsonProperty]
        public SKU SKU
        {
            get
            {
                if (_SKU == null)
                {
                    _SKU = SKU.Parse(this);
                }
                return _SKU;
            }
            set
            {
                _SKU = value;
                if (value == null)
                    RootElement.Remove("sku");
                else
                    RootElement["sku"] = JObject.Parse(value.ToString());
            }
        }
        [JsonProperty]
        public string Kind
        {
            get
            {
                if (_Kind == null)
                {
                    if (RootElement.TryGetValue("kind", out JToken kind))
                        _Kind = _Functions.Evaluate(kind.Value<string>(), FullContext, $"{RootElement.Path}.kind").ToString();
                    _Kind = string.Empty;
                }
                return _Kind;
            }
            set
            {
                _Kind = value;
                if (value == null)
                    RootElement.Remove("kind");
                else
                    RootElement["kind"] = value;
            }
        }
        [JsonProperty]
        public Plan Plan
        {
            get
            {
                if (_Plan == null)
                {
                    if (RootElement.TryGetValue("plan", out JToken planE))
                        _Plan = new Plan(planE as JObject, FullContext);
                }
                return _Plan;
            }
            set
            {
                _Plan = value;
                if (value == null)
                    RootElement.Remove("plan");
                else
                    RootElement["plan"] = JObject.Parse(value.ToString());
            }
        }
        [JsonProperty]
        public List<string> Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = new List<string>();
                    if (RootElement.TryGetValue("zones", out JToken zones))
                    {
                        if (zones.Type == JTokenType.Array)
                        {
                            foreach (var z in zones.Children())
                            {
                                _Zones.Add(_Functions.Evaluate(z.Value<string>(), FullContext).ToString());
                            }
                        }
                        else if (zones.Type == JTokenType.String)
                        {
                            if (!(_Functions.Evaluate(zones.Value<string>(), FullContext) is JsonValue arr) || arr.ValueKind != JsonValueKind.Array)
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
            set
            {
                _Zones = value;
                if (value == null)
                    RootElement.Remove("zones");
                else
                    RootElement["zones"] = JArray.FromObject(value);
            }
        }
        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
        // You can't use a copy loop for a child resource.
        [JsonProperty]
        public ResourceCollection Resources
        {
            get
            {
                if (_Resources == null)
                {
                    if (this.RootElement.TryGetValue("resources", out JToken resourcesE))
                    {
                        _Resources = new ResourceCollection(resourcesE as JArray, this.FullContext, this.FullName, this.FullType);
                    }
                    else
                    {
                        var d = new JArray();
                        this.RootElement.Add(new JProperty("resources", d));
                        _Resources = new ResourceCollection(d, this.FullContext, this.FullName, this.FullType);
                    }
                }
                return _Resources;
            }
            set
            {
                _Resources = value;
                if (value == null)
                    RootElement.Remove("resources");
                else
                    RootElement["resources"] = JArray.Parse(value.ToString());
            }
        }
        [JsonProperty]
        public Copy Copy
        {
            get
            {
                if (_Copy == null)
                {
                    if (RootElement.TryGetValue("copy", out JToken copy))
                        _Copy = new Copy(copy as JObject, FullContext, this);
                }
                return _Copy;

            }
            set
            {
                _Copy = value;
                if (value == null)
                    RootElement.Remove("copy");
                else
                {
                    _Copy.Resource = this;
                    RootElement["copy"] = JObject.Parse(value.ToString());
                }
            }
        }
        [JsonProperty]
        public string ResourceGroup
        {
            get
            {
                if (_resourceGroup == null)
                {
                    if (RootElement.TryGetValue("resourceGroup", out JToken resourceGroup))
                        _resourceGroup = _Functions.Evaluate(resourceGroup.Value<string>(), FullContext, $"{RootElement.Path}.resourceGroup").ToString();
                    else
                        _resourceGroup = (FullContext[ContextKeys.ARM_CONTEXT] as Deployment).ResourceGroup;
                }
                return _resourceGroup;
            }
            set
            {
                _resourceGroup = value;
                RootElement["resourceGroup"] = value;
            }
        }
        [JsonProperty]
        public string SubscriptionId
        {
            get
            {
                if (_SubscriptionId == null)
                {
                    if (RootElement.TryGetValue("subscriptionId", out JToken subscriptionId))
                        _SubscriptionId = _Functions.Evaluate(subscriptionId.Value<string>(), FullContext, $"{RootElement.Path}.subscriptionId").ToString();
                    else
                        _SubscriptionId = (FullContext[ContextKeys.ARM_CONTEXT] as Deployment).SubscriptionId;
                }
                return _SubscriptionId;
            }
            set
            {
                _SubscriptionId = value;
                RootElement["subscriptionId"] = value;
            }
        }
        [JsonProperty]
        public string ManagementGroupId
        {
            get
            {
                if (_ManagementGroupId == null)
                {
                    if (RootElement.TryGetValue("managementGroupId", out JToken managementGroupId))
                        _ManagementGroupId = _Functions.Evaluate(managementGroupId.Value<string>(), FullContext, $"{RootElement.Path}.managementGroupId").ToString();
                    else
                        _ManagementGroupId = (FullContext[ContextKeys.ARM_CONTEXT] as Deployment).ManagementGroupId;
                }
                return _ManagementGroupId;
            }
            set
            {
                _ManagementGroupId = value;
                RootElement["managementGroupId"] = value;
            }
        }
        #endregion

        #region
        [JsonProperty] protected readonly string _ParentName;
        [JsonProperty] protected readonly string _ParentType;
        /// <summary>
        /// if CopyIndex has value, means this is a expanded resource
        /// </summary>
        [JsonProperty] public int? CopyIndex { get; set; }
        #endregion

        public string ParentResourceId
        {
            get
            {
                return CopyIndex.HasValue ? Copy.Id : string.IsNullOrEmpty(_ParentName) ? Deployment.ResourceId : Deployment.GetFirstResource(GetNameWithServiceType(_ParentType, _ParentName)).ResourceId;
            }
        }

        private Dictionary<string, object> _FullContext;

        internal override Dictionary<string, object> FullContext
        {
            get
            {
                if (_FullContext == null)
                {
                    _FullContext = new Dictionary<string, object>();
                    if (_ParentContext != null)
                    {
                        foreach (var item in _ParentContext)
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

        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        public string FullType { get { return string.IsNullOrEmpty(this._ParentType) ? this.Type : $"{this._ParentType}/{this.Type}"; } }

        public string FullName
        {
            get
            {
                return string.IsNullOrEmpty(this._ParentName) ? this.Name : $"{this._ParentName}/{this.Name}";
            }
        }
        private string GetNameWithServiceType(string type, string name)
        {
            var ns = name.Split('/');
            var ts = type.Split('/');
            string nestr = string.Empty;
            for (int i = 1; i < ns.Length; i++)
            {
                nestr += $"/{ts[i + 1]}/{ns[i]}";
            }
            return $"{ts[0]}/{ts[1]}/{ns[0]}{nestr}";
        }
        public string NameWithServiceType
        {
            get
            {
                return GetNameWithServiceType(FullType, FullName);
            }
        }
        public string RawProperties
        {
            get
            {
                if (RootElement.TryGetValue("properties", out JToken p))
                    return p.ToString();
                return "{}";
            }
            set
            {
                RootElement["properties"] = JObject.Parse(value);
            }
        }

        private bool _PropertiesNeedReload = false;

        // todo: when switch IsRuntime property of deployment, this maybe need be call to refresh the property
        // 考虑当 IsRuntime属性发生变化时自动刷新
        public void Refresh()
        {
            _PropertiesNeedReload = true;
            _Condition = null;
            _ApiVersion = null;
            _Type = null;
            _Name = null;
            _Location = null;
            _Comments = null;
            _DependsOn = null;
            _Properties = null;
            _SKU = null;
            _Kind = null;
            _Plan = null;
            _Zones = null;
            _Copy = null;
            _resourceGroup = null;
            _SubscriptionId = null;
            _ManagementGroupId = null;

        }

        private void LazyLoadDependsOnAnProperties()
        {
            _PropertiesNeedReload = false;
            _DependsOn = new DependsOnCollection();
            if (RootElement.TryGetValue("dependsOn", out JToken dependsOn))
            {
                string dep;
                if (dependsOn.Type == JTokenType.String)
                    dep = _Functions.Evaluate(dependsOn.Value<string>(), FullContext, $"{RootElement}.dependsOn").ToString();
                else if (dependsOn.Type == JTokenType.Array)
                    dep = dependsOn.ToString();
                else
                    throw new Exception("dependsON should be an arrary");
                using var dd = JsonDocument.Parse(dep);
                int index = 0;
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    _DependsOn.Add(_Functions.Evaluate(item.GetString(), FullContext, $"{RootElement.Path}.dependsOn[{index}]").ToString(), Deployment);
                    index++;
                }
            }
            // if there has Implicit dependency by reference function in properties
            // the reference function should be evaluate at provisioning time
            using var doc = JsonDocument.Parse(RawProperties);
            _Properties = doc.RootElement.ExpandObject(FullContext, $"{RootElement.Path}.properties");
            if (FullContext.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
            {
                _DependsOn.AddRange(conditionDep as List<string>, Deployment);
                FullContext.Remove(ContextKeys.DEPENDSON);
            }
            if (!string.IsNullOrEmpty(this._ParentName))
            {
                _DependsOn.Add(GetNameWithServiceType(_ParentType, _ParentName), Deployment);
            }

        }

        public string ResourceId
        {
            get
            {
                if (Copy != null && !CopyIndex.HasValue)
                    return Copy.Id;
                if (Deployment.Template.DeployLevel == DeployLevel.ResourceGroup)
                {
                    List<object> pars = new List<object>{
                        SubscriptionId,
                        ResourceGroup,
                        FullType};
                    pars.AddRange(FullName.Split('/'));
                    return _Functions.ResourceId(Deployment, pars.ToArray());
                }
                if (Deployment.Template.DeployLevel == DeployLevel.Subscription)
                {
                    List<object> pars = new List<object> { SubscriptionId, FullType };
                    pars.AddRange(FullName.Split('/'));
                    return _Functions.SubscriptionResourceId(Deployment, pars.ToArray());
                }
                List<object> pars1 = new List<object> { FullType };
                pars1.AddRange(FullName.Split('/'));
                return _Functions.TenantResourceId(pars1.ToArray());
            }
        }


        public IEnumerable<Resource> FlatEnumerateChild()
        {
            if (this.Resources == null)
                yield break;
            foreach (var child in this.Resources)
            {
                yield return child;
                foreach (var nest in child.FlatEnumerateChild())
                {
                    yield return nest;
                }
            }
        }

        public override string ToString()
        {
            return RootElement.ToString(Formatting.Indented);
        }
        public void Change(string content)
        {
            this.RootElement.Replace(JObject.Parse(content));
        }
    }
}