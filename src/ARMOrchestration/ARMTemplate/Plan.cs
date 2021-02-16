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
    public class Plan 
    {
        private readonly ARMFunctions _Functions;
        private JObject RootElement;
        private Dictionary<string, object> _FullContext;
        public Plan(JObject element, Dictionary<string, object> fullContext)
        {
            this.RootElement = element;
            this._Functions = (fullContext[ContextKeys.ARM_CONTEXT] as Deployment).ServiceProvider.GetService<ARMFunctions>();
            this._FullContext = fullContext;
        }

        private string _Name = null;

        [DisplayName("name")]
        public string Name
        {
            get
            {
                if (_Name == null)
                {
                    if (!RootElement.TryGetValue("name", out JToken nameE))
                        throw new Exception("cannot find name property in paln node");
                    _Name = this._Functions.Evaluate(nameE.Value<string>(), _FullContext,$"{RootElement.Path}.name").ToString();
                }
                return _Name;
            }
        }

        private string _PromotionCode = null;

        [DisplayName("promotionCode")]
        public string PromotionCode
        {
            get
            {
                if (_PromotionCode == null)
                {
                    if (RootElement.TryGetValue("promotionCode", out JToken promotionCodeE))
                        _PromotionCode = this._Functions.Evaluate(promotionCodeE.Value<string>(), _FullContext,$"{RootElement.Path}.promotionCode").ToString();
                    else
                        _PromotionCode = string.Empty;
                }
                return _PromotionCode;
            }
        }

        private string _Publisher = null;

        [DisplayName("publisher")]
        public string Publisher
        {
            get
            {
                if (_Publisher == null)
                {
                    if (RootElement.TryGetValue("publisher", out JToken publisherE))
                        _Publisher = this._Functions.Evaluate(publisherE.Value<string>(), _FullContext,$"{RootElement.Path}.publisher").ToString();
                    else
                        _Publisher = string.Empty;
                }
                return _Publisher;
            }
        }

        private string _Product = null;

        [DisplayName("product")]
        public string Product
        {
            get
            {
                if (_Product == null)
                {
                    if (!RootElement.TryGetValue("product", out JToken productE))
                        _Product = this._Functions.Evaluate(productE.Value<string>(), _FullContext,$"{RootElement.Path}.product").ToString();
                    else
                        _Product = string.Empty;
                }
                return _Product;
            }
        }

        private string _Version = null;

        [DisplayName("version")]
        public string Version
        {
            get
            {
                if (_Version == null)
                {
                    if (RootElement.TryGetValue("version", out JToken versionE))
                        _Version = this._Functions.Evaluate(versionE.Value<string>(), _FullContext).ToString();
                    else
                        _Version = string.Empty;
                }
                return _Version;
            }
        }

        internal (bool, string) Validate()
        {
            object _;
            try
            {
                if (!RootElement.TryGetValue("name", out JToken type))
                    return (false, "cannot find name property in paln node");
                _ = this.Product;
                _ = this.PromotionCode;
                _ = this.Publisher;
                _ = this.Version;
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            return (true, string.Empty);
        }
    }
}