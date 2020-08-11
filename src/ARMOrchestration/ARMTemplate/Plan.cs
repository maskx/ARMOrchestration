using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Plan
    {
        private JsonElement RootElement;
        private Dictionary<string, object> FullContext;
        private ARMFunctions _Functions;

        public Plan(JsonElement element, Dictionary<string, object> fullContext)
        {
            this.RootElement = element;
            this.FullContext = fullContext;
            this._Functions = (fullContext[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput).ServiceProvider.GetService<ARMFunctions>();
        }

        private string _Name = null;

        public string Name
        {
            get
            {
                if (_Name == null)
                {
                    if (!RootElement.TryGetProperty("name", out JsonElement nameE))
                        throw new Exception("cannot find name property in paln node");
                    _Name = this._Functions.Evaluate(nameE.GetString(), FullContext).ToString();
                }
                return _Name;
            }
        }

        private string _PromotionCode = null;

        public string PromotionCode
        {
            get
            {
                if (_PromotionCode == null)
                {
                    if (RootElement.TryGetProperty("promotionCode", out JsonElement promotionCodeE))
                        _PromotionCode = this._Functions.Evaluate(promotionCodeE.GetString(), FullContext).ToString();
                    else
                        _PromotionCode = string.Empty;
                }
                return _PromotionCode;
            }
        }

        private string _Publisher = null;

        public string Publisher
        {
            get
            {
                if (_Publisher == null)
                {
                    if (RootElement.TryGetProperty("publisher", out JsonElement publisherE))
                        _Publisher = this._Functions.Evaluate(publisherE.GetString(), FullContext).ToString();
                    else
                        _Publisher = string.Empty;
                }
                return _Publisher;
            }
        }

        private string _Product = null;

        public string Product
        {
            get
            {
                if (_Product == null)
                {
                    if (!RootElement.TryGetProperty("product", out JsonElement productE))
                        _Product = this._Functions.Evaluate(productE.GetString(), FullContext).ToString();
                    else
                        _Product = string.Empty;
                }
                return _Product;
            }
        }

        private string _Version = null;

        public string Version
        {
            get
            {
                if (_Version == null)
                {
                    if (RootElement.TryGetProperty("version", out JsonElement versionE))
                        _Version = this._Functions.Evaluate(versionE.GetString(), FullContext).ToString();
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
                if (!RootElement.TryGetProperty("name", out JsonElement type))
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