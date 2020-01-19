using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestration : TaskOrchestration<TaskResult, string>
    {
        private TemplateOrchestrationOptions options;

        public TemplateOrchestration(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            TemplateOrchestrationInput input = this.DataConverter.Deserialize<TemplateOrchestrationInput>(arg);
            string rtv = string.Empty;
            var valid = await context.ScheduleTask<TaskResult>(typeof(ValidateTemplateActivity), input);
            if (valid.Code != 200)
                return valid;
            var deploymentContext = DataConverter.Deserialize<DeploymentContext>(valid.Content);

            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("armcontext", deploymentContext);

            if (input.Mode.ToLower() == "complete")
            {
            }
            else
            {
                #region ResourceGroup ReadOnly Lock Check

                if (options.GetCheckLockRequestInput != null)
                {
                    TaskResult readonlyLockCheckResult;

                    if (deploymentContext.Template.DeployLevel == ARMTemplate.Template.ResourceGroupDeploymentLevel)
                    {
                        readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       options.GetCheckLockRequestInput(
                                           ARMFunctions.Evaluate($"[subscriptionresourceid('{options.BuitinServiceTypes.ResourceGroup}','{input.ResourceGroup}')]", armContext).ToString(),
                                           "readonly"));
                    }
                    else if (deploymentContext.Template.DeployLevel == ARMTemplate.Template.SubscriptionDeploymentLevel)
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

                #region Provisioning resources

                List<Task> tasks = new List<Task>();

                foreach (var resource in deploymentContext.Template.Resources)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = resource.ToString(),
                        OrchestrationContext = armContext,
                        DeploymentId = context.OrchestrationInstance.InstanceId,
                        CorrelationId = input.CorrelationId
                    };

                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                }
                foreach (var item in deploymentContext.Template.Copys)
                {
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        typeof(CopyOrchestration),
                        new CopyOrchestrationInput()
                        {
                            Copy = item.Value,
                            Context = deploymentContext
                        }));
                }
                await Task.WhenAll(tasks.ToArray());
            }

            #endregion Provisioning resources

            #region get template outputs

            if (!string.IsNullOrEmpty(deploymentContext.Template.Outputs))
            {
                rtv = ARMFunctions.GetOutputs(deploymentContext.Template.Outputs, armContext);
            }

            #endregion get template outputs

            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}