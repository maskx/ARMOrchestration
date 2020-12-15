using DurableTask.Core;
using DurableTask.Core.Exceptions;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class RequestOrchestration<T> : TaskOrchestration<TaskResult, AsyncRequestActivityInput, TaskResult, string>
        where T:CommunicationJob,new()
    {
        public const string Name = "RequestOrchestration";
        private string eventName = string.Empty;
        private TaskCompletionSource<TaskResult> waitHandler = null;
        private readonly ARMTemplateHelper templateHelper;

        public RequestOrchestration(ARMTemplateHelper templateHelper)
        {
            this.templateHelper = templateHelper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, AsyncRequestActivityInput input)
        {
            this.eventName = input.ProvisioningStage.ToString();
            this.waitHandler = new TaskCompletionSource<TaskResult>();
            try
            {
                await context.ScheduleTask<TaskResult>(AsyncRequestActivity<T>.Name, "1.0", input);
            }
            catch (TaskFailedException ex)
            {
                var response = new ErrorResponse()
                {
                    Code = $"{AsyncRequestActivity<T>.Name}:{input.ProvisioningStage}",
                    Message = ex.Message,
                    AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                };
                templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation()
                {
                    InstanceId = input.InstanceId,
                    DeploymentId = input.DeploymentId,
                    Stage = (ProvisioningStage)(0 - input.ProvisioningStage),
                    Result = DataConverter.Serialize(response)
                });
                return new TaskResult() { Code = 500, Content = response };
            }

            await waitHandler.Task;
            var r = waitHandler.Task.Result;
            templateHelper.SaveDeploymentOperation(new DeploymentOperation()
            {
                InstanceId = input.InstanceId,
                DeploymentId = input.DeploymentId,
                Stage = r.Code == 200 ? input.ProvisioningStage : (ProvisioningStage)(0 - input.ProvisioningStage),
                Result = DataConverter.Serialize(waitHandler.Task.Result)
            });
            return r;
        }

        public override void OnEvent(OrchestrationContext context, string name, TaskResult input)
        {
            if (this.waitHandler != null && name == this.eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}