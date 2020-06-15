﻿using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
            if ("copy".Equals(property.Name, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    // TODO: add validate
                    var copyResult = helper.ParseCopy(item.GetRawText(), context).Result;
                    if (!copyResult.Result)
                        return (false, copyResult.Message);
                    var copy = copyResult.Copy;
                    using JsonDocument doc = JsonDocument.Parse(copy.Input);
                    var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>();
                    copyContext.Add("copyindex", copyindex);
                    copyContext.Add("currentloopname", copy.Name);
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