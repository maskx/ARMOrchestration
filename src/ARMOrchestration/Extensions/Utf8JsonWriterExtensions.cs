using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.Extensions
{
    public static class Utf8JsonWriterExtensions
    {
        public static void WriteElement(this Utf8JsonWriter writer, JsonElement element, Dictionary<string, object> context, ARMTemplateHelper helper)
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
                        writer.WriteProperty(p, context, helper);
                    }
                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var a in element.EnumerateArray())
                    {
                        writer.WriteElement(a, context, helper);
                    }
                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    var r = helper.ARMfunctions.Evaluate(element.GetString(), context);
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

        public static (bool Result, string Message) WriteProperty(this Utf8JsonWriter writer, JsonProperty property, Dictionary<string, object> context, ARMTemplateHelper helper)
        {
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-numeric#copyindex
            if ("copy".Equals(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                var copyProperty = property.Value;
                // this is for Variable  and Property iteration
                if (copyProperty.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        var (Result, Message, Copy) = helper.ParseCopy(item.GetRawText(), context);
                        if (!Result)
                            return (false, Message);
                        var copy = Copy;
                        using JsonDocument doc = JsonDocument.Parse(copy.Input);
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
                        for (int i = 0; i < copy.Count; i++)
                        {
                            copyindex[copy.Name] = i;
                            writer.WriteElement(doc.RootElement, copyContext, helper);
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
                    int count;
                    if (countProperty.ValueKind == JsonValueKind.Number)
                        count = countProperty.GetInt32();
                    else if (countProperty.ValueKind == JsonValueKind.String)
                        count = (int)helper.ARMfunctions.Evaluate(countProperty.GetString(), context);
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
                        writer.WriteElement(input, copyContext, helper);
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    throw new Exception("the structer of copy property is wrong");
                }
            }
            else
            {
                writer.WritePropertyName(property.Name);
                writer.WriteElement(property.Value, context, helper);
            }
            return (true, string.Empty);
        }
    }
}