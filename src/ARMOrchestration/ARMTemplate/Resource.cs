using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
    public class Resource : ChangeTracking
    {
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

        public Deployment Input { get; set; }

        [DisplayName("$DeploymentOperationId")]
        public string DeploymentOperationId { get; private set; }

        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        internal IServiceProvider ServiceProvider { get { return Input.ServiceProvider; } }

        public Resource()
        {
        }

        public Resource(string rawString, Dictionary<string, object> fullContext, string deploymentOperationId, int index)
        {
            Deployment input = fullContext[ContextKeys.ARM_CONTEXT] as Deployment;
            this.RawString = rawString;
            this.Input = input;
            this.ParentContext = new Dictionary<string, object>();
            foreach (var item in fullContext)
            {
                if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                ParentContext.Add(item.Key, item.Value);
            }
            this.CopyIndex = index;
            this.DeploymentOperationId = $"{deploymentOperationId}:{index}";
            base.Change(this.DeploymentOperationId, "$DeploymentOperationId");
        }
        public Resource(string rawString, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        {
            Deployment input = fullContext[ContextKeys.ARM_CONTEXT] as Deployment;
            this.RawString = rawString;
            this._ParentName = parentName;
            this._ParentType = parentType;
            this.Input = input;
            this.ParentContext = new Dictionary<string, object>();
            foreach (var item in fullContext)
            {
                if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                ParentContext.Add(item.Key, item.Value);
            }
            if (this.RootElement.TryGetProperty("$DeploymentOperationId", out JsonElement operationId))
            {
                this.DeploymentOperationId = operationId.GetString();
            }
            else
            {
                this.DeploymentOperationId = Guid.NewGuid().ToString("N");
                this.Change(this.DeploymentOperationId, "$DeploymentOperationId");
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
                        var r = Input.GetFirstResource(_ParentName);
                        _Condition = r.Condition;
                    }
                    else if (RootElement.TryGetProperty("condition", out JsonElement condition))
                    {
                        if (condition.ValueKind == JsonValueKind.False)
                            _Condition = false;
                        else if (condition.ValueKind == JsonValueKind.True)
                            _Condition = true;
                        else if (condition.ValueKind == JsonValueKind.String)
                            _Condition = (bool)this._Functions.Evaluate(condition.GetString(), FullContext);
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
                    if (RootElement.TryGetProperty("apiVersion", out JsonElement apiVersion))
                        _ApiVersion = apiVersion.GetString();
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
                    if (RootElement.TryGetProperty("type", out JsonElement type))
                        _Type = type.GetString();
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
                    if (!RootElement.TryGetProperty("name", out JsonElement name))
                        throw new Exception("not find name in resource node");
                    if (Copy != null && !CopyIndex.HasValue)
                        _Name = name.GetString();
                    else
                        _Name = this._Functions.Evaluate(name.GetString(), FullContext).ToString();
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
        public string NameWithServiceType
        {
            get
            {
                var ns = FullName.Split('/');
                var ts = FullType.Split('/');
                string nestr = string.Empty;
                for (int i = 1; i < ns.Length; i++)
                {
                    nestr += $"/{ts[i + 1]}/{ns[i]}";
                }
                return $"{ts[0]}/{ts[1]}/{ns[0]}{nestr}";
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
                    if (RootElement.TryGetProperty("location", out JsonElement location))
                        _Location = this._Functions.Evaluate(location.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("comments", out JsonElement comments))
                        _Comments = comments.GetString();
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
                if (_Properties == null || _PropertiesNeedReload)
                    LazyLoadDependsOnAnProperties();
                return _Properties;
            }
        }

        private bool _PropertiesNeedReload = false;

        // todo: when switch IsRuntime property of deployment, this maybe need be call to refresh the property
        // 考虑当 IsRuntime属性发生变化时自动刷新
        public void Refresh()
        {
            _PropertiesNeedReload = true;
        }
        public ChangeTracking _RawProperties;

        [DisplayName("properties")]
        public ChangeTracking RawProperties
        {
            get
            {
                if (_RawProperties == null)
                {
                    if (RootElement.TryGetProperty("properties", out JsonElement properties))
                    {
                        _RawProperties = properties.GetRawText();
                    }
                    else _RawProperties = new ChangeTracking();
                }
                return _RawProperties;
            }
            set
            {
                _RawProperties = value;
                Change(value, "properties");
                _PropertiesNeedReload = true;
            }
        }

        private void LazyLoadDependsOnAnProperties()
        {
            _PropertiesNeedReload = false;
            if (_DependsOn == null)
            {
                _DependsOn = new DependsOnCollection();
                if (RootElement.TryGetProperty("dependsOn", out JsonElement dependsOn))
                {
                    string dep;
                    if (dependsOn.ValueKind == JsonValueKind.String)
                        dep = _Functions.Evaluate(dependsOn.GetString(), FullContext).ToString();
                    else if (dependsOn.ValueKind == JsonValueKind.Array)
                        dep = dependsOn.GetRawText();
                    else
                        throw new Exception("dependsON should be an arrary");
                    using var dd = JsonDocument.Parse(dep);
                    foreach (var item in dd.RootElement.EnumerateArray())
                    {
                        _DependsOn.Add(_Functions.Evaluate(item.GetString(), FullContext).ToString(), Input);
                    }
                }
            }
            var infrastructure = ServiceProvider.GetService<IInfrastructure>();
            if (this.Type == infrastructure.BuiltinServiceTypes.Deployments)
                _Properties = RawProperties.RawString;
            else if (Copy != null && !CopyIndex.HasValue)
            {
                _Properties = RawProperties.RawString;
            }
            else
            {
                _Properties = RawProperties.RootElement.ExpandObject(FullContext, _Functions, infrastructure);
                // if there has Implicit dependency by reference function in properties
                // the reference function should be evaluate at provisioning time
                // so keep the original text
                if (FullContext.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
                {
                    _DependsOn.AddRange(conditionDep as List<string>, Input);
                    FullContext.Remove(ContextKeys.DEPENDSON);
                }
            }
        }

        private SKU _SKU;

        // todo: support modify
        [DisplayName("sku")]
        public SKU SKU
        {
            get
            {
                if (_SKU == null)
                {
                    if (RootElement.TryGetProperty("sku", out JsonElement sku))
                        _SKU = SKU.Parse(sku, _Functions, FullContext);
                    else
                        _SKU = new SKU() { Name = SKU.Default };
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
                    if (RootElement.TryGetProperty("kind", out JsonElement kind))
                        _Kind = _Functions.Evaluate(kind.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("plan", out JsonElement planE))
                        _Plan = new Plan(planE, FullContext);
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
                            if (!(_Functions.Evaluate(zones.GetString(), FullContext) is JsonValue arr) || arr.ValueKind != JsonValueKind.Array)
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
                    if (RootElement.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                        _resourceGroup = _Functions.Evaluate(resourceGroup.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                        _SubscriptionId = _Functions.Evaluate(subscriptionId.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("managementGroupId", out JsonElement managementGroupId))
                        _ManagementGroupId = _Functions.Evaluate(managementGroupId.GetString(), FullContext).ToString();
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

        private Copy _Copy = null;
        public Copy Copy
        {
            get
            {
                if (_Copy == null)
                {
                    if (RootElement.TryGetProperty("copy", out JsonElement copy))
                        _Copy = new Copy(copy.GetRawText(), FullContext, this);
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
                    if (this.RootElement.TryGetProperty("resources", out JsonElement resourcesE))
                    {
                        _Resources = new ResourceCollection(resourcesE.GetRawText(), this.FullContext, this.FullName, this.FullType);
                    }
                    else
                    {
                        _Resources = new ResourceCollection("[]", this.FullContext, this.FullName, this.FullType);
                    }
                }
                return _Resources;
            }
        }

        internal (bool, string) Validate()
        {
            try
            {
                if (!RootElement.TryGetProperty("type", out JsonElement type))
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

        public override void Change(object value, string name)
        {
            if (this.CopyIndex.HasValue)
            {
                this.Input.Template.ChangedCopyResoures.Add(this);
            }
            base.Change(value, name);
        }
    }
}