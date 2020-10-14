using DurableTask.Core;
using DurableTask.Core.Exceptions;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using System;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class RequestOrchestration : TaskOrchestration<TaskResult, AsyncRequestActivityInput, TaskResult, string>
    {
        public const string Name = "RequestOrchestration";
        private string eventName = string.Empty;
        private TaskCompletionSource<TaskResult> waitHandler = null;
        private readonly ARMTemplateHelper templateHelper;
        private readonly IServiceProvider _ServiceProvider;

        public RequestOrchestration(
            ARMTemplateHelper templateHelper,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.templateHelper = templateHelper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, AsyncRequestActivityInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            this.eventName = input.ProvisioningStage.ToString();
            this.waitHandler = new TaskCompletionSource<TaskResult>();
            try
            {
                await context.ScheduleTask<TaskResult>(AsyncRequestActivity.Name, "1.0", input);
            }
            catch (TaskFailedException ex)
            {
                var response = DataConverter.Serialize(new ErrorResponse()
                {
                    Code = $"{AsyncRequestActivity.Name}:{input.ProvisioningStage}",
                    Message = ex.Message,
                    AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                });
                templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                {
                    InstanceId = input.InstanceId,
                    ExecutionId = input.ExecutionId,
                    Stage = (ProvisioningStage)(0 - input.ProvisioningStage),
                    Result = response
                });
                return new TaskResult() { Code = 500, Content = response };
            }

            await waitHandler.Task;
            var r = waitHandler.Task.Result;
            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
            {
                InstanceId = input.InstanceId,
                ExecutionId = input.ExecutionId,
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