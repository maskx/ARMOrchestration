using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration : TaskOrchestration<TaskResult, string>
    {
        private ARMOrchestrationOptions ARMOptions;
        private IServiceProvider serviceProvider;
        private readonly ARMFunctions functions;

        public DeploymentOrchestration(
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider,
            ARMFunctions functions)
        {
            this.ARMOptions = options?.Value;
            this.serviceProvider = serviceProvider;
            this.functions = functions;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            DeploymentOrchestrationInput input = this.DataConverter.Deserialize<DeploymentOrchestrationInput>(arg);

            string rtv = string.Empty;

            #region validate template

            var valid = await context.ScheduleTask<TaskResult>(typeof(ValidateTemplateActivity), input);
            if (valid.Code != 200)
                return valid;

            #endregion validate template

            var deploymentContext = DataConverter.Deserialize<DeploymentContext>(valid.Content);

            #region check permission

            var permissionResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                            typeof(RequestOrchestration),
                            new RequestOrchestrationInput()
                            {
                                DeploymentContext = deploymentContext,
                                RequestAction = RequestAction.CheckPermission
                            });
            if (permissionResult.Code != 200)
            {
                return permissionResult;
            }

            #endregion check permission

            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                { "armcontext", deploymentContext}
            };

            #region ResourceGroup ReadOnly Lock Check

            //there are only resource group level lock
            if (deploymentContext.Template.DeployLevel == DeployLevel.ResourceGroup)
            {
                var readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                        typeof(RequestOrchestration),
                        new RequestOrchestrationInput()
                        {
                            DeploymentContext = deploymentContext,
                            RequestAction = RequestAction.CheckLock
                        });
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

            #endregion Provisioning resources

            if (input.Mode == DeploymentMode.Complete)
            {
                // TODO: complete mode, delete resource not exist in template
            }

            #region get template outputs

            if (!string.IsNullOrEmpty(deploymentContext.Template.Outputs))
            {
                rtv = this.functions.GetOutputs(deploymentContext.Template.Outputs, armContext);
            }

            #endregion get template outputs

            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}