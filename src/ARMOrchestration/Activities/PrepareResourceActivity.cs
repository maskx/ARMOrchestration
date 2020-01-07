using DurableTask.Core;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Activities
{
    public class PrepareResourceActivity : TaskActivity<ResourceOrchestrationInput, TaskResult>
    {
        protected override TaskResult Execute(TaskContext context, ResourceOrchestrationInput input)
        {
            using JsonDocument doc = JsonDocument.Parse(input.Resource);
            var root = doc.RootElement;
            if (!root.TryGetProperty("apiVersion", out JsonElement apiVersion))
                return new TaskResult() { Code = 400, Content = "no apiVersion defined in template" };
            if (!root.TryGetProperty("type", out JsonElement @type))
                return new TaskResult() { Code = 400, Content = "no type defined in template" };
            if (!root.TryGetProperty("name", out JsonElement name))
                return new TaskResult() { Code = 400, Content = "no name defined in template" };

            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteString("apiVersion", apiVersion.GetString());
            writer.WriteString("type", @type.GetString());
            writer.WriteString("name", ARMFunctions.Evaluate(name.GetString(), input.OrchestrationContext).ToString());
            if (root.TryGetProperty("condition", out JsonElement condition))
            {
                writer.WritePropertyName("condition");
                if (condition.ValueKind == JsonValueKind.String)
                    writer.WriteBooleanValue((bool)ARMFunctions.Evaluate(condition.GetString(), input.OrchestrationContext));
                else if (condition.ValueKind == JsonValueKind.True)
                    writer.WriteBooleanValue(true);
                else if (condition.ValueKind == JsonValueKind.False)
                    writer.WriteBooleanValue(false);
                else writer.WriteBooleanValue(true);
            }
            else
            {
                writer.WriteBoolean("condition", true);
            }
            if (root.TryGetProperty("location", out JsonElement location))
            {
                writer.WriteString("location", ARMFunctions.Evaluate(location.GetString(), input.OrchestrationContext).ToString());
            }
            if (root.TryGetProperty("tags", out JsonElement tags))
            {
                writer.WritePropertyName("tags");
                tags.WriteTo(writer);
            }
            // copy can be removed, because copy has  been processed in copyorchestration
            //if (root.TryGetProperty("copy", out JsonElement copy))
            //{
            //    writer.WritePropertyName("copy");
            //    copy.WriteTo(writer);
            //}
            if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                writer.WritePropertyName("dependsOn");
                dependsOn.WriteTo(writer);
            }
            if (root.TryGetProperty("properties", out JsonElement properties))
            {
                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                foreach (var property in properties.EnumerateObject())
                {
                    writer.WriteProperty(property, input.OrchestrationContext);
                }
                writer.WriteEndObject();
            }
            if (root.TryGetProperty("sku", out JsonElement sku))
            {
                writer.WritePropertyName("sku");
                sku.WriteTo(writer);
            }
            if (root.TryGetProperty("kind", out JsonElement kind))
            {
                writer.WritePropertyName("kind");
                kind.WriteTo(writer);
            }
            if (root.TryGetProperty("plan", out JsonElement plan))
            {
                writer.WritePropertyName("plan");
                plan.WriteTo(writer);
            }
            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                writer.WritePropertyName("resources");
                resources.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
            return new TaskResult() { Code = 200, Content = Encoding.UTF8.GetString(ms.ToArray()) };
        }
    }
}