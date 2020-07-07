using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivity : AsyncTaskActivity<AsyncRequestActivityInput, TaskResult>
    {
        public const string Name ="AsyncRequestActivity";
        private readonly ARMTemplateHelper templateHelper;
        private readonly maskx.OrchestrationService.Activity.AsyncRequestActivity asyncRequestActivity;
        private readonly IInfrastructure infrastructure;

        public AsyncRequestActivity(IOptions<CommunicationWorkerOptions> options,
            ARMTemplateHelper templateHelper,
            IInfrastructure infrastructure)
        {
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
            asyncRequestActivity = new maskx.OrchestrationService.Activity.AsyncRequestActivity(options);
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, AsyncRequestActivityInput input)
        {
            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentContext, infrastructure, input.Resource)
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