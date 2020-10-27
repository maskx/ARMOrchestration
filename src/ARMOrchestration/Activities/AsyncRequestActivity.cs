using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivity : AsyncTaskActivity<AsyncRequestActivityInput, TaskResult>
    {
        public const string Name = "AsyncRequestActivity";
        private readonly ARMTemplateHelper templateHelper;
        private readonly OrchestrationService.Activity.AsyncRequestActivity asyncRequestActivity;
        private readonly IInfrastructure infrastructure;
        private readonly IServiceProvider _ServiceProvider;

        public AsyncRequestActivity(IOptions<CommunicationWorkerOptions> options,
            ARMTemplateHelper templateHelper,
            IInfrastructure infrastructure,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
            asyncRequestActivity = new OrchestrationService.Activity.AsyncRequestActivity(options);
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, AsyncRequestActivityInput input)
        {
            templateHelper.SaveDeploymentOperation(new DeploymentOperation()
            {
                InstanceId = input.InstanceId,
                ExecutionId = input.ExecutionId,
                Stage = input.ProvisioningStage
            });
            await asyncRequestActivity.SaveRequest(infrastructure.GetRequestInput(input),
                new OrchestrationInstance()
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId
                });
            return new TaskResult() { Code = 200 };
        }
    }
}