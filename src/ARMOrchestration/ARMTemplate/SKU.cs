using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class SKU
    {
        public string Name { get; set; }
        public string Tier { get; set; }
        public string Size { get; set; }
        public string Family { get; set; }
        public string Capacity { get; set; }

        public static SKU Parse(string rawString,ARMFunctions functions,Dictionary<string,object> context)
        {
            using var doc= JsonDocument.Parse(rawString);
            var root = doc.RootElement;
            SKU sku = new SKU();
            if (root.TryGetProperty("name", out JsonElement nameE))
            {
                sku.Name = functions.Evaluate(nameE.GetString(), context).ToString();
            }
            else
                throw new Exception("cannot find name property in SKU node");
            if (root.TryGetProperty("tier", out JsonElement tierE))
                sku.Name = functions.Evaluate(tierE.GetString(),context).ToString();
            if (root.TryGetProperty("size", out JsonElement sizeE))
                sku.Size = functions.Evaluate(sizeE.GetString(), context).ToString();
            if (root.TryGetProperty("family", out JsonElement familyE))
                sku.Family = functions.Evaluate(familyE.GetString(), context).ToString();
            if (root.TryGetProperty("capacity", out JsonElement capacityE))
                sku.Capacity = capacityE.GetRawText();
            return sku;
        }
    }
}
