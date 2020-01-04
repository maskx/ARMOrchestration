using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestration : TaskOrchestration<TaskResult, string>
    {
        private TemplateOrchestrationOptions options;

        public TemplateOrchestration(
            IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            TemplateOrchestrationInput input = this.DataConverter.Deserialize<TemplateOrchestrationInput>(arg);
            string rtv = string.Empty;
            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("armcontext", input);
            var template = new ARMTemplate.Template(input.Template, armContext);

            if (input.Mode.ToLower() == "complete")
            {
            }
            else
            {
                #region ResourceGroup ReadOnly Lock Check

                if (options.GetCheckLockRequestInput != null)
                {
                    TaskResult readonlyLockCheckResult;

                    if (template.DeployLevel == ARMTemplate.Template.ResourceGroupDeploymentLevel)
                    {
                        readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       options.GetCheckLockRequestInput(
                                           ARMFunctions.Evaluate($"[subscriptionresourceid('{options.BuitinServiceTypes.ResourceGroup}','{input.ResourceGroup}')]", armContext).ToString(),
                                           "readonly"));
                    }
                    else if (template.DeployLevel == ARMTemplate.Template.SubscriptionDeploymentLevel)
                    {
                        readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                      typeof(AsyncRequestOrchestration),
                                      options.GetCheckLockRequestInput(
                                          ARMFunctions.Evaluate($"[tenantresourceid('{options.BuitinServiceTypes.Subscription}','{input.SubscriptionId}')]", armContext).ToString(),
                                          "readonly"));
                    }
                    else
                    {
                        //there no Tenant level lock
                        readonlyLockCheckResult = new TaskResult() { Code = 404 };
                    }
                    if (readonlyLockCheckResult.Code != 404)
                        return readonlyLockCheckResult;
                }

                #endregion ResourceGroup ReadOnly Lock Check

                List<Task> tasks = new List<Task>();

                foreach (var resource in template.Resources)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = resource.ToString(),
                        OrchestrationContext = armContext,
                        DeploymentId = context.OrchestrationInstance.InstanceId,
                        CorrelationId = input.CorrelationId
                    };
                    if (null == resource.Copy)
                    {
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                    }
                    else
                    {
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(GroupOrchestration), p));
                    }
                }
                await Task.WhenAll(tasks.ToArray());

                if (!string.IsNullOrEmpty(template.Outputs))
                {
                    rtv = ARMFunctions.GetOutputs(template.Outputs, armContext);
                }
            }
            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}