using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivity<T> : AsyncTaskActivity<AsyncRequestActivityInput, TaskResult> where T : CommunicationJob, new()
    {
        public const string Name = "AsyncRequestActivity";
        private readonly ARMTemplateHelper templateHelper;
        private readonly OrchestrationService.Activity.AsyncRequestActivity<T> asyncRequestActivity;
        private readonly IInfrastructure infrastructure;

        public AsyncRequestActivity(IOptions<CommunicationWorkerOptions> options,
            ARMTemplateHelper templateHelper,
            IInfrastructure infrastructure)
        {
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
            asyncRequestActivity = new OrchestrationService.Activity.AsyncRequestActivity<T>(options);
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, AsyncRequestActivityInput input)
        {
            templateHelper.SaveDeploymentOperation(new DeploymentOperation()
            {
                InstanceId = input.InstanceId,
                ExecutionId = input.ExecutionId,
                Stage = input.ProvisioningStage
            });
            var request = (T)infrastructure.GetRequestInput(input);
            request.EventName = input.ProvisioningStage.ToString();
            request.InstanceId = context.OrchestrationInstance.InstanceId;
            request.ExecutionId = context.OrchestrationInstance.ExecutionId;
            await asyncRequestActivity.SaveRequest(request);
            return new TaskResult() { Code = 200 };
        }
    }
}