using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using maskx.OrchestrationService.Activity;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestration : TaskOrchestration<TaskResult, string>
    {
        private ARMOrchestrationOptions ARMOptions;
        private IServiceProvider serviceProvider;

        public TemplateOrchestration(
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider)
        {
            this.ARMOptions = options?.Value;
            this.serviceProvider = serviceProvider;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            TemplateOrchestrationInput input = this.DataConverter.Deserialize<TemplateOrchestrationInput>(arg);
            string rtv = string.Empty;
            if (string.IsNullOrEmpty(input.DeploymentId))
                input.DeploymentId = context.OrchestrationInstance.InstanceId;
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

                AsyncRequestInput lockResult;

                TaskResult readonlyLockCheckResult;

                if (deploymentContext.Template.DeployLevel == ARMTemplate.Template.ResourceGroupDeploymentLevel)
                {
                    lockResult = ARMOptions.GetRequestInput(
                        serviceProvider,
                        deploymentContext,
                        new ARMTemplate.Resource()
                        {
                            ResouceId = ARMFunctions.Evaluate($"[subscriptionresourceid('{ARMOptions.BuitinServiceTypes.ResourceGroup}','{input.ResourceGroup}')]", armContext).ToString()
                        },
                        "locks", "readonly");
                    readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       lockResult);
                }
                else
                {
                    //there are only resource group level lock
                    readonlyLockCheckResult = new TaskResult() { Code = 404 };
                }

                if (readonlyLockCheckResult.Code != 404)
                    return readonlyLockCheckResult;

                #endregion ResourceGroup ReadOnly Lock Check

                #region Provisioning resources

                List<Task> tasks = new List<Task>();

                foreach (var resource in deploymentContext.Template.Resources)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = resource.Value,
                        Context = deploymentContext,
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