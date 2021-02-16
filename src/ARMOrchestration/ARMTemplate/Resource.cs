using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Resource : ObjectChangeTracking
    {
        public string ParentResourceId
        {
            get
            {
                return CopyIndex.HasValue ? Copy.Id : string.IsNullOrEmpty(_ParentName) ? Deployment.ResourceId : Deployment.GetFirstResource(GetNameWithServiceType(_ParentType, _ParentName)).ResourceId;
            }
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

        internal override Dictionary<string, object> FullContext
        {
            get
            {
                if (_FullContext == null)
                {
                    _FullContext = new Dictionary<string, object> {
                        {ContextKeys.ARM_CONTEXT,this.Deployment} };
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

        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        public Resource()
        {
        }

        public Resource(JObject root, Dictionary<string, object> fullContext,  int index) : base(root, fullContext)
        {
            this.ParentContext = new Dictionary<string, object>();
            foreach (var item in fullContext)
            {
                if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                ParentContext.Add(item.Key, item.Value);
            }
            this.CopyIndex = index;
      
        }
        public Resource(JObject root, Dictionary<string, object> fullContext, string parentName = null, string parentType = null) : base(root, fullContext)
        {
            this._ParentName = parentName;
            this._ParentType = parentType;
            this.ParentContext = new Dictionary<string, object>();
            foreach (var item in fullContext)
            {
                if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                ParentContext.Add(item.Key, item.Value);
            }
        }

        private bool? _Condition;

        [DisplayName("condition")]
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
                Change(value, "condition");
            }
        }

        private string _ApiVersion = null;

        [DisplayName("apiVersion")]
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
                Change(value, "apiVersion");
            }
        }

        private string _Type = null;

        /// <summary>
        /// the type set in template
        /// </summary>
        [DisplayName("type")]
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
                Change(value, "type");
            }
        }
        public string FullType { get { return string.IsNullOrEmpty(this._ParentType) ? this.Type : $"{this._ParentType}/{this.Type}"; } }

        private string _Name = null;

        /// <summary>
        /// the name set in template
        /// </summary>
        [DisplayName("name")]
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
                Change(value, "name");
            }
        }
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

        private string _Location = null;

        [DisplayName("location")]
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
                Change(value, "location");
            }
        }

        private string _Comments = null;

        [DisplayName("comments")]
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
                Change(value, "comments");
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
        [DisplayName("dependsOn")]
        public DependsOnCollection DependsOn
        {
            get
            {
                if (_DependsOn == null || _PropertiesNeedReload)
                    LazyLoadDependsOnAnProperties();
                return _DependsOn;
            }
        }
        private string _Properties;
        public string Properties
        {
            get
            {
                if (string.IsNullOrEmpty(_Properties) || _PropertiesNeedReload)
                    LazyLoadDependsOnAnProperties();
                return _Properties;
            }
        }
        public string RawProperties
        {
            get
            {
                if (Deployment.ChangeMap.TryGetValue($"{RootElement.Path}.properties", out object v))
                    return v.ToString();
                if (RootElement.TryGetValue("properties", out JToken p))
                    return p.ToString();
                return "{}";
            }
            set
            {
                Deployment.ChangeMap.Add($"{RootElement.Path}.properties", value);
            }
        }

        private bool _PropertiesNeedReload = false;

        // todo: when switch IsRuntime property of deployment, this maybe need be call to refresh the property
        // 考虑当 IsRuntime属性发生变化时自动刷新
        public void Refresh()
        {
            _PropertiesNeedReload = true;
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

            var infrastructure = ServiceProvider.GetService<IInfrastructure>();
            if (this.Type == infrastructure.BuiltinServiceTypes.Deployments)
            {
                // Implicit dependsOn only generate in deployment, cannot cross deployment

            }
            else if (Copy != null && !CopyIndex.HasValue)
            {
                // copy's dependsOn will create in expand resource
            }
            else
            {
                // if there has Implicit dependency by reference function in properties
                // the reference function should be evaluate at provisioning time
                using var doc = JsonDocument.Parse(RawProperties);
                _Properties = doc.RootElement.ExpandObject(FullContext, $"{RootElement.Path}.properties");
                if (FullContext.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
                {
                    _DependsOn.AddRange(conditionDep as List<string>, Deployment);
                    FullContext.Remove(ContextKeys.DEPENDSON);
                }
            }
            if (!string.IsNullOrEmpty(this._ParentName))
            {
                _DependsOn.Add(GetNameWithServiceType(_ParentType, _ParentName), Deployment);
            }
        }

        private SKU _SKU;

        [DisplayName("sku")]
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
        }

        private string _Kind;

        [DisplayName("kind")]
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
                Change(value, "kind");
            }
        }

        private Plan _Plan = null;

        [DisplayName("plan")]
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
                Change(value, "plan");
            }
        }

        private ZoneCollection _Zones;

        // todo: support modify
        [DisplayName("zones")]
        public ZoneCollection Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = new ZoneCollection();
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
        }

        private string _resourceGroup = null;

        [DisplayName("resourceGroup")]
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
                Change(value, "resourceGroup");
            }
        }

        private string _SubscriptionId = null;

        [DisplayName("subscriptionId")]
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
                Change(value, "subscriptionId");
            }
        }

        public string _ManagementGroupId = null;

        [DisplayName("managementGroupId")]
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
                Change(value, "managementGroupId");
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

        private Copy _Copy = null;
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
        }

        private ResourceCollection _Resources;
        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
        // You can't use a copy loop for a child resource.
        [DisplayName("resources")]
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
                        _Resources = new ResourceCollection();
                    }
                }
                return _Resources;
            }
        }

        internal (bool, string) Validate()
        {
            try
            {
                if (!RootElement.TryGetValue("type", out JToken type))
                    return (false, "not find type in resource node");
                string msg;
                bool rtv;
                if (this.Copy != null && !this.CopyIndex.HasValue)
                {
                    foreach (var r in this.Copy.EnumerateResource())
                    {
                        (rtv, msg) = r.Validate();
                        if (!rtv) return (rtv, msg);
                    }
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
                        (rtv, msg) = this.Plan.Validate();
                        if (!rtv) return (rtv, msg);
                    }
                    // validate properties and dependson and parameter and variables
                    LazyLoadDependsOnAnProperties();
                    // validate child
                    if (this.Resources != null)
                    {
                        foreach (var child in this.Resources)
                        {
                            (rtv, msg) = child.Validate();
                            if (!rtv)
                                return (rtv, msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (true, string.Empty);
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
    }
}