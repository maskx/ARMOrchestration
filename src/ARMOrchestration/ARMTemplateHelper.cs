using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;

        public ARMTemplateHelper(IOptions<ARMOrchestrationOptions> options)
        {
            this.options = options?.Value;
        }

        public (bool Result, string Message, Template Template) ValidateTemplate(TemplateOrchestrationInput input)
        {
            Template template = new Template();
            DeploymentContext deploymentContext = new DeploymentContext()
            {
                CorrelationId = input.CorrelationId,
                DeploymentId = input.DeploymentId,
                Mode = input.Mode,
                ResourceGroup = input.ResourceGroup,
                SubscriptionId = input.SubscriptionId,
                TenantId = input.TenantId,
                Parameters = input.Parameters,
                Template = template
            };
            using JsonDocument doc = JsonDocument.Parse(input.Template);
            var root = doc.RootElement;
            if (!root.TryGetProperty("$schema", out JsonElement schema))
                return (false, "not find $schema in template", null);
            if (!root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                return (false, "not find contentVersion in template", null);
            if (!root.TryGetProperty("resources", out JsonElement resources))
                return (false, "not find resources in template", null);
            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                {"armcontext", deploymentContext} };

            template.Schema = schema.GetString();
            template.ContentVersion = contentVersion.GetString();

            if (root.TryGetProperty("apiProfile", out JsonElement apiProfile))
                template.ApiProfile = apiProfile.GetString();
            if (root.TryGetProperty("parameters", out JsonElement parameters))
                template.Parameters = parameters.GetRawText();
            if (root.TryGetProperty("outputs", out JsonElement outputs))
                template.Outputs = outputs.GetRawText();
            if (root.TryGetProperty("variables", out JsonElement variables))
            {
                // cos var can reference var
                template.Variables = variables.GetRawText();
                template.Variables = variables.ExpandObject(armContext);
            }

            if (root.TryGetProperty("functions", out JsonElement functions))
            {
                var fr = Functions.Parse(functions.GetRawText());
                if (fr.Result)
                    template.Functions = fr.Functions;
                else
                    return (false, fr.Message, null);
            }
            foreach (var resource in resources.EnumerateArray())
            {
                if (resource.TryGetProperty("copy", out JsonElement copy))
                {
                    var c = Copy.Parse(copy.GetRawText(), armContext);
                    var copyResult = resource.ExpandCopyResource(c.Copy, armContext, options);
                    if (copyResult.Result)
                    {
                        c.Copy.Resources = copyResult.Resources;
                        template.Copys.Add(c.Copy.Name, c.Copy);
                    }
                    else
                        return (false, copyResult.Message, null);
                }
                else
                {
                    var resResult = Resource.Parse(resource.GetRawText(), armContext, this.options);
                    if (resResult.Result)
                        template.Resources.Add(resResult.resource.ResouceId, resResult.resource);
                    else
                        return (false, resResult.Message, null);
                }
            }
            return (true, string.Empty, template);
        }

        public WhatIfOperationResult WhatIf(PredictTemplateOrchestrationInput input)
        {
            var result = new WhatIfOperationResult();
            var valid = ValidateTemplate(new TemplateOrchestrationInput()
            {
                CorrelationId = input.CorrelationId,
                Parameters = input.Parameters,
                ResourceGroup = input.ResourceGroupName,
                SubscriptionId = input.SubscriptionId,
                Template = input.Template,
                TenantId = input.TenantId
            });
            if (!valid.Result)
            {
                result.Status = "failed";
                result.Error = new ErrorResponse() { Code = "400", Message = valid.Message };
            }
            string queryScope;
            if (input.ScopeType == ScopeTypeEnum.ResourceGroup)
                queryScope = $"subscriptions/{input.SubscriptionId}/resourceGroups/{input.ResourceGroupName}/resources";
            else
                queryScope = $"subscriptions/{input.SubscriptionId}/resources";
            var str = this.options.ListFunction(queryScope, valid.Template.ApiProfile);
            using var doc = JsonDocument.Parse(str.Content);
            foreach (var r in doc.RootElement.EnumerateArray())
            {
                if (!r.TryGetProperty("id", out JsonElement id))
                    break;
                id.GetString();
            }
            result.Status = "succeeded";
            return result;
        }
    }
}