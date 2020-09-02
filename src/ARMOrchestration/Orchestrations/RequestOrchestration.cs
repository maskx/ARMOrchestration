using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using System;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class RequestOrchestration : TaskOrchestration<TaskResult, AsyncRequestActivityInput>
    {
        public const string Name = "RequestOrchestration";
        private string eventName = string.Empty;
        private TaskCompletionSource<string> waitHandler = null;
        private readonly IInfrastructure infrastructure;
        private readonly ARMTemplateHelper templateHelper;
        private readonly IServiceProvider _ServiceProvider;

        public RequestOrchestration(
            IInfrastructure infrastructure,
            ARMTemplateHelper templateHelper,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, AsyncRequestActivityInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            this.eventName = input.ProvisioningStage.ToString();
            this.waitHandler = new TaskCompletionSource<string>();
            await context.ScheduleTask<TaskResult>(AsyncRequestActivity.Name, "1.0", input);
            await waitHandler.Task;
            var r = DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
            {
                InstanceId = input.InstanceId,
                ExecutionId = input.ExecutionId,
                Stage = r.Code == 200 ? input.ProvisioningStage : (ProvisioningStage)(0 - input.ProvisioningStage),
                Result = waitHandler.Task.Result
            });
            return r;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == this.eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}