using DurableTask.Core;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class DeploymentOperationActivity : TaskActivity<DeploymentOperation, TaskResult>
    {
        private readonly ARMTemplateHelper templateHelper;

        public DeploymentOperationActivity(ARMTemplateHelper templateHelper)
        {
            this.templateHelper = templateHelper;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOperation input)
        {
            return ExecuteAsync(context, input).Result;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, DeploymentOperation input)
        {
            templateHelper.SaveDeploymentOperation(input);
            return new TaskResult() { Code = 200 };
        }
    }
}