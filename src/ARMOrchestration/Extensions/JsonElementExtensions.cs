﻿using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Extensions
{
    public class JsonElementEqualityComparer : IEqualityComparer<JsonElement>
    {
        public bool Equals(JsonElement x, JsonElement y)
        {
            return x.IsEqual(y);
        }

        public int GetHashCode(JsonElement obj)
        {
            int rtv = 0;
            if (obj.ValueKind is JsonValueKind.Array)
            {
                rtv = obj.EnumerateArray().Sum((e) =>
                  {
                      return GetHashCode(e);
                  });
            }
            else if (obj.ValueKind == JsonValueKind.Object)
            {
                rtv = obj.EnumerateObject().Sum((p) =>
                  {
                      return p.Name.GetHashCode() + GetHashCode(p.Value);
                  });
            }
            else
            {
                rtv = obj.GetRawText().GetHashCode();
            }
            return rtv;
        }
    }

    public static class JsonElementExtensions
    {
        public static bool IsEqual(this JsonElement self, JsonElement target)
        {
            if (self.ValueKind != target.ValueKind)
                return false;
            switch (self.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var item in self.EnumerateObject())
                    {
                        if (!target.TryGetProperty(item.Name, out JsonElement e))
                            return false;
                        if (!item.Value.IsEqual(e))
                            return false;
                    }
                    return true;

                case JsonValueKind.Array:
                    if (self.GetArrayLength() != target.GetArrayLength())
                        return false;
                    for (int i = 0; i < self.GetArrayLength(); i++)
                    {
                        if (!self[i].IsEqual(target[i]))
                            return false;
                    }
                    return true;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                    return self.GetRawText() == target.GetRawText();
            }
            return false;
        }

        public static IEnumerable<object> Intersect(this JsonElement self, JsonElement target)
        {
            if (self.ValueKind == JsonValueKind.Array)
            {
                self.EnumerateArray().Intersect(target.EnumerateArray(), new JsonElementEqualityComparer());
            }
            else if (self.ValueKind == JsonValueKind.Object)
            {
                self.EnumerateObject().Intersect(target.EnumerateObject(), new JsonPropertyEqualityComparer());
            }
            return null;
        }

        public static string ExpandObject(this JsonElement self, Dictionary<string, object> context, ARMFunctions functions, IInfrastructure infrastructure) 
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            foreach (var item in self.EnumerateObject())
            {
                writer.WriteProperty(item, context, functions, infrastructure);
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static string ExpadCopy(this JsonElement copyProperty, Dictionary<string, object> context, ARMFunctions functions, IInfrastructure infrastructure)
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.ExpandCopy(copyProperty, context, functions, infrastructure);
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}