using maskx.ARMOrchestration.ARMTemplate;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.Extensions
{
    public static class Utf8JsonWriterExtensions
    {
        public static void WriteElement(this Utf8JsonWriter writer, JsonElement element, Dictionary<string, object> context)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Undefined:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    element.WriteTo(writer);
                    break;

                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var p in element.EnumerateObject())
                    {
                        writer.WriteProperty(p, context);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var a in element.EnumerateArray())
                    {
                        writer.WriteElement(a, context);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    var r = ARMFunctions.Evaluate(element.GetString(), context);
                    if (r is JsonValue j)
                        j.RootElement.WriteTo(writer);
                    else if (r is bool b)
                        writer.WriteBooleanValue(b);
                    else if (r is string s)
                        writer.WriteStringValue(s);
                    else if (r is int i)
                        writer.WriteNumberValue(i);
                    else
                        writer.WriteNullValue();
                    break;

                default:
                    break;
            }
        }

        public static (bool Result, string Message) WriteProperty(this Utf8JsonWriter writer, JsonProperty property, Dictionary<string, object> context)
        {
            if ("copy".Equals(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    // TODO: add validate
                    var copyResult = Copy.Parse(item.GetRawText(), context);
                    if (!copyResult.Result)
                        return (false, copyResult.Message);
                    var copy = copyResult.Copy;
                    using JsonDocument doc = JsonDocument.Parse(copy.Input);
                    var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>();
                    copyContext.Add("armcontext", context["armcontext"]);
                    copyContext.Add("copyindex", copyindex);
                    copyContext.Add("currentloopname", copy.Name);
                    writer.WritePropertyName(copy.Name);
                    writer.WriteStartArray();
                    for (int i = 0; i < copy.Count; i++)
                    {
                        copyindex[copy.Name] = i;
                        writer.WriteElement(doc.RootElement, copyContext);
                    }
                    writer.WriteEndArray();
                }
            }
            else
            {
                writer.WritePropertyName(property.Name);
                writer.WriteElement(property.Value, context);
            }
            return (true, string.Empty);
        }
    }
}