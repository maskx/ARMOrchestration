using DurableTask.Core;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Activities
{
    public class PrepareTemplateActivity : TaskActivity<TemplateOrchestrationInput, TaskResult>
    {
        protected override TaskResult Execute(TaskContext context, TemplateOrchestrationInput input)
        {
            using JsonDocument doc = JsonDocument.Parse(input.Template);
            var root = doc.RootElement;
            if (!root.TryGetProperty("$schema", out JsonElement schema))
                return new TaskResult() { Code = 400, Content = "no $schema defined in template" };
            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                return new TaskResult() { Code = 400, Content = "no contentVersion defined in template" };
            if (!root.TryGetProperty("resources", out JsonElement resources))
                return new TaskResult() { Code = 400, Content = "no resources defined in template" };
            if (!root.TryGetProperty("variables", out JsonElement variables))
                return new TaskResult() { Code = 200, Content = input.Template };
            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                { "armcontext", input}
            };
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteString("$schema", schema.GetString());
            writer.WriteString("contentVersion", contentVersion.GetString());
            writer.WritePropertyName("variables");
            writer.WriteStartObject();
            foreach (var item in variables.EnumerateObject())
            {
                writer.WriteProperty(item, armContext);
            }
            writer.WriteEndObject();
            writer.WritePropertyName("resources");
            resources.WriteTo(writer);
            if (root.TryGetProperty("apiProfile", out JsonElement apiProfile))
            {
                writer.WriteString("apiProfile", apiProfile.GetString());
            }
            if (root.TryGetProperty("parameters", out JsonElement parameters))
            {
                writer.WritePropertyName("parameters");
                parameters.WriteTo(writer);
            }
            if (root.TryGetProperty("functions", out JsonElement functions))
            {
                writer.WritePropertyName("functions");
                functions.WriteTo(writer);
            }
            if (root.TryGetProperty("outputs", out JsonElement outputs))
            {
                writer.WritePropertyName("outputs");
                outputs.WriteTo(writer);
            }

            writer.WriteEndObject();
            writer.Flush();
            return new TaskResult() { Code = 200, Content = Encoding.UTF8.GetString(ms.ToArray()) };
        }

        //    private JToken Parse(JsonElement e, Dictionary<string, object> context)
        //    {
        //        switch (e.ValueKind)
        //        {
        //            case JsonValueKind.Undefined:
        //                return JValue.CreateUndefined();

        //            case JsonValueKind.Object:
        //                JObject @object = new JObject();
        //                foreach (var item in e.EnumerateObject())
        //                {
        //                    @object.Add(Parse(item, context));
        //                }
        //                return @object;

        //            case JsonValueKind.Array:
        //                JArray array = new JArray();
        //                foreach (var item in e.EnumerateArray())
        //                {
        //                    array.Add(Parse(item, context));
        //                }
        //                return array;

        //            case JsonValueKind.String:
        //                return new JValue(ARMFunctions.Evaluate(e.GetString(), context));

        //            case JsonValueKind.Number:
        //                return new JValue(e.GetInt32());

        //            case JsonValueKind.True:
        //                return new JValue(true);

        //            case JsonValueKind.False:
        //                return new JValue(false);

        //            case JsonValueKind.Null:
        //                return JValue.CreateNull();

        //            default:
        //                return null;
        //        }
        //    }

        //    private JProperty Parse(JsonProperty property, Dictionary<string, object> context)
        //    {
        //        if ("copy".Equals(property.Name, StringComparison.OrdinalIgnoreCase))
        //        {
        //            var copy = new Copy(property.Value.GetRawText(), context);
        //            JArray array = new JArray();
        //            JsonDocument doc = JsonDocument.Parse(copy.Input);
        //            var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
        //            Dictionary<string, object> copyContext = new Dictionary<string, object>();
        //            copyContext.Add("armcontext", input);
        //            copyContext.Add("copyindex", copyindex);
        //            copyContext.Add("currentloopname", copy.Name);
        //            for (int i = 0; i < copy.Count; i++)
        //            {
        //                copyindex[copy.Name] = i;
        //                array.Add(Parse(doc.RootElement, copyContext);
        //            }
        //            return new JProperty(copy.Name, array); ;
        //        }
        //        else
        //        {
        //            switch (property.Value.ValueKind)
        //            {
        //                case JsonValueKind.Undefined:
        //                    return new JProperty(property.Name, JValue.CreateUndefined());

        //                case JsonValueKind.Object:
        //                    JObject @object = new JObject();
        //                    foreach (var item in property.Value.EnumerateObject())
        //                    {
        //                        @object.Add(Parse(item, context));
        //                    }
        //                    return new JProperty(property.Name, @object);

        //                case JsonValueKind.Array:
        //                    JArray array = new JArray();
        //                    foreach (var item in property.Value.EnumerateArray())
        //                    {
        //                        array.Add(Parse(item, context));
        //                    }
        //                    return new JProperty(property.Name, array);

        //                case JsonValueKind.String:
        //                    return new JProperty(property.Name, new JValue(ARMFunctions.Evaluate(property.Value.GetString(), context)));

        //                case JsonValueKind.Number:
        //                    return new JProperty(property.Name, property.Value.GetInt32());

        //                case JsonValueKind.True:
        //                    return new JProperty(property.Name, true);

        //                case JsonValueKind.False:
        //                    return new JProperty(property.Name, false);

        //                case JsonValueKind.Null:
        //                    return new JProperty(property.Name, JValue.CreateNull());

        //                default:
        //                    return null;
        //            }
        //        }
        //    }
    }
}