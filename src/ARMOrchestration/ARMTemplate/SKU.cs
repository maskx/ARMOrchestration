using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class SKU
    {
        public const string Default = "Default";
        public string Name { get; set; } = Default;
        public string Tier { get; set; }
        public string Size { get; set; }
        public string Family { get; set; }
        public string Capacity { get; set; }
        public static SKU Parse(string rawString, ARMFunctions functions, Dictionary<string, object> context)
        {
            using var doc = JsonDocument.Parse(rawString);
            return Parse(doc.RootElement, functions, context);
        }
        public static SKU Parse(JsonElement root, ARMFunctions functions, Dictionary<string, object> context)
        {
            SKU sku = new SKU();
            if (root.TryGetProperty("name", out JsonElement nameE))
                sku.Name = functions.Evaluate(nameE.GetString(), context).ToString();
            else
                throw new Exception("cannot find name property in SKU node");
            if (root.TryGetProperty("tier", out JsonElement tierE))
                sku.Name = functions.Evaluate(tierE.GetString(), context).ToString();
            if (root.TryGetProperty("size", out JsonElement sizeE))
                sku.Size = functions.Evaluate(sizeE.GetString(), context).ToString();
            if (root.TryGetProperty("family", out JsonElement familyE))
                sku.Family = functions.Evaluate(familyE.GetString(), context).ToString();
            if (root.TryGetProperty("capacity", out JsonElement capacityE))
                sku.Capacity = capacityE.GetRawText();
            return sku;
        }
        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteString("name", this.Name);
            if (!string.IsNullOrEmpty(this.Tier))
                writer.WriteString("tier", this.Tier);
            if (!string.IsNullOrEmpty(this.Size))
                writer.WriteString("size", this.Size);
            if (!string.IsNullOrEmpty(this.Family))
                writer.WriteString("family", this.Family);
            if (!string.IsNullOrEmpty(this.Capacity))
                writer.WriteString("capacity", this.Capacity);
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
