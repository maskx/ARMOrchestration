using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.Extensions
{
    public static class Utf8JsonWriterExtensions
    {
        public static void WriteElement(this Utf8JsonWriter writer, JsonElement element, Dictionary<string, object> context, string path)
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
                        writer.WriteProperty(p, context, path);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var a in element.EnumerateArray())
                    {
                        writer.WriteElement(a, context, path);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    var r = (context[ContextKeys.ARM_CONTEXT] as Deployment).Functions.Evaluate(element.GetString(), context, path);
                    if (r is JsonValue j)
                        j.RootElement.WriteTo(writer);
                    else if (r is bool b)
                        writer.WriteBooleanValue(b);
                    else if (r is string s)
                        writer.WriteStringValue(s);
                    else if (r is Int32 i)
                        writer.WriteNumberValue(i);
                    else if (r is FakeJsonValue)
                        writer.WriteStringValue("fakeString");
                    else
                        writer.WriteNullValue();
                    break;

                default:
                    break;
            }
        }

        public static void WriteRawString(this Utf8JsonWriter writer, string rawString)
        {
            using var doc = JsonDocument.Parse(rawString);
            doc.RootElement.WriteTo(writer);
        }

        public static void WriteRawString(this Utf8JsonWriter writer, string name, string rawString)
        {
            writer.WritePropertyName(name);
            writer.WriteRawString(rawString);
        }

        public static void ExpandCopy(this Utf8JsonWriter writer, JsonElement copyProperty, Dictionary<string, object> context, string path)
        {
            // this is for Variable  and Property iteration
            if (copyProperty.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in copyProperty.EnumerateArray())
                {
                    Copy copy = copy = new Copy(JObject.Parse(item.GetRawText()), context);
                    var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>
                        {
                            { "copyindex", copyindex },
                            { "currentloopname", copy.Name }
                        };
                    foreach (var k in context.Keys)
                    {
                        copyContext.Add(k, context[k]);
                    }
                    writer.WritePropertyName(copy.Name);
                    writer.WriteStartArray();
                    if (!item.TryGetProperty("input", out JsonElement inputE))
                    {
                        throw new Exception($"cannot find input property in path: {path}");
                    }
                    for (int i = 0; i < copy.Count; i++)
                    {
                        copyindex[copy.Name] = i;
                        writer.WriteElement(inputE, copyContext, path);
                    }

                    writer.WriteEndArray();
                    if (copyContext.TryGetValue(ContextKeys.DEPENDSON, out object copyDependsOn))
                    {
                        List<string> dependsOn;
                        if (context.TryGetValue(ContextKeys.DEPENDSON, out object d))
                        {
                            dependsOn = d as List<string>;
                        }
                        else
                        {
                            dependsOn = new List<string>();
                            context.Add(ContextKeys.DEPENDSON, dependsOn);
                        }
                        dependsOn.AddRange(copyDependsOn as List<string>);
                    }
                    if (copyContext.ContainsKey(ContextKeys.NEED_REEVALUATE))
                        context.TryAdd(ContextKeys.NEED_REEVALUATE, true);
                }
            }
            // this is for output
            else if (copyProperty.ValueKind == JsonValueKind.Object)
            {
                var input = copyProperty.GetProperty("input");
                var countProperty = copyProperty.GetProperty("count");
                var deployment = context[ContextKeys.ARM_CONTEXT] as Deployment;
                int count;
                if (countProperty.ValueKind == JsonValueKind.Number)
                    count = countProperty.GetInt32();
                else if (countProperty.ValueKind == JsonValueKind.String)
                    count = (int)deployment.Functions.Evaluate(countProperty.GetString(), context);
                else
                    throw new Exception("the property of count has wrong error. It should be number or an function return a number");
                var name = Guid.NewGuid().ToString("N");
                var copyindex = new Dictionary<string, int>() { { name, 0 } };
                Dictionary<string, object> copyContext = new Dictionary<string, object>
                    {
                        { "copyindex", copyindex },
                        { "currentloopname", name }
                    };
                foreach (var k in context.Keys)
                {
                    copyContext.Add(k, context[k]);
                }
                writer.WritePropertyName("value");
                writer.WriteStartArray();
                for (int i = 0; i < count; i++)
                {
                    copyindex[name] = i;
                    writer.WriteElement(input, copyContext, path);
                }
                writer.WriteEndArray();
            }
            else
            {
                throw new Exception("the structer of copy property is wrong");
            }
        }

        public static (bool Result, string Message) WriteProperty(this Utf8JsonWriter writer, JsonProperty property, Dictionary<string, object> context, string path)
        {
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-numeric#copyindex
            if ("copy".Equals(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                writer.ExpandCopy(property.Value, context, $"{path}.copy");
            }
            else
            {
                writer.WritePropertyName(property.Name);
                writer.WriteElement(property.Value, context, $"{path}.{ property.Name}");
            }
            return (true, string.Empty);
        }
    }
}