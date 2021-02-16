using Dynamitey.DynamicObjects;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class SKU : ObjectChangeTracking
    {
        public const string Default = "Default";
        private string _Name;
        [DisplayName("name")]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_Name))
                {
                    if (RootElement.TryGetValue("name", out JToken nameE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Name = nameE.Value<string>();
                        else
                            _Name = _Functions.Evaluate(nameE.Value<string>(), FullContext).ToString();
                    }
                    else
                    {
                        _Name = Default;
                    }
                }
                return _Name;
            }
            set
            {
                _Name = value;
                Change(value, "name");
            }
        }
        private string _Tier;
        [DisplayName("tier")]
        public string Tier
        {
            get
            {
                if (string.IsNullOrEmpty(_Tier))
                {
                    if (RootElement.TryGetValue("tier", out JToken tierE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Tier = tierE.Value<string>();
                        else
                            _Tier = _Functions.Evaluate(tierE.Value<string>(), FullContext).ToString();
                    }
                }
                return _Tier;
            }
            set
            {
                _Tier = value;
                Change(value, "tier");
            }
        }
        private string _Size;
        [DisplayName("size")]
        public string Size
        {
            get
            {
                if (string.IsNullOrEmpty(_Size))
                {
                    if (RootElement.TryGetValue("size", out JToken sizeE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Size = sizeE.Value<string>();
                        else
                            _Size = _Functions.Evaluate(sizeE.Value<string>(), FullContext).ToString();
                    }
                }
                return _Size;
            }
            set
            {
                _Size = value;
                Change(value, "size");
            }
        }
        private string _Family;
        [DisplayName("family")]
        public string Family
        {
            get
            {
                if (string.IsNullOrEmpty(_Family))
                {
                    if (RootElement.TryGetValue("family", out JToken familyE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Family = familyE.Value<string>();
                        else
                            _Family = _Functions.Evaluate(familyE.Value<string>(), FullContext).ToString();
                    }
                }
                return _Family;
            }
            set
            {
                _Family = value;
                Change(value, "family");
            }
        }
        private string _Capacity;
        [DisplayName("capacity")]
        public string Capacity
        {
            get
            {
                if (string.IsNullOrEmpty(_Capacity))
                {
                    if (RootElement.TryGetValue("capacity", out JToken capacityE))
                    {
                        _Capacity = capacityE.ToString();
                    }
                }
                return _Capacity;
            }
            set
            {
                _Capacity = value;
                Change(value, "capacity");
            }
        }
        private Resource _Resource;

        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        public SKU() { }
        public SKU(JObject root, Dictionary<string, object> context) : base(root, context) { }
        public static SKU Parse(Resource resource)
        {
            if (!resource.RootElement.TryGetValue("sku", out JToken sku))
                return null;
            var s = new SKU(sku as JObject, resource.FullContext);
            s._Resource = resource;
            return s;
        }

    }
}
