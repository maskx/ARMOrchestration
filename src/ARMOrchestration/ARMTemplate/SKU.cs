using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class SKU : ChangeTracking
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
                    if (RootElement.TryGetProperty("name", out JsonElement nameE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Name = nameE.GetString();
                        else
                            _Name = _Functions.Evaluate(nameE.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("tier", out JsonElement tierE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Tier = tierE.GetString();
                        else
                            _Tier = _Functions.Evaluate(tierE.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("size", out JsonElement sizeE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Size = sizeE.GetString();
                        else
                            _Size = _Functions.Evaluate(sizeE.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("family", out JsonElement familyE))
                    {
                        if (_Resource.Copy != null && !_Resource.CopyIndex.HasValue)
                            _Family = familyE.GetString();
                        else
                            _Family = _Functions.Evaluate(familyE.GetString(), FullContext).ToString();
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
                    if (RootElement.TryGetProperty("capacity", out JsonElement capacityE))
                    {
                        _Capacity = capacityE.GetRawText();
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
        internal Dictionary<string, object> FullContext
        {
            get { return _Resource.FullContext; }
        }
        public Deployment Input { get { return _Resource.Input; } }
        protected ARMFunctions _Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }

        internal IServiceProvider ServiceProvider { get { return Input.ServiceProvider; } }
        public SKU() { }
        public SKU(Resource resource)
        {
            _Resource = resource;
            if (resource.RootElement.TryGetProperty("sku", out JsonElement sku))
                this.RawString = sku.GetRawText();
            else
                this.RawString = "{}";

        }

    }
}
