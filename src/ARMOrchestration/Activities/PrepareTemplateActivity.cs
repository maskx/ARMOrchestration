using DurableTask.Core;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
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
    }
}