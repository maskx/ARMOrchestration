using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class RequestOrchestration : TaskOrchestration<TaskResult, RequestOrchestrationInput>
    {
        private const string eventName = "CommunicationEvent";
        private TaskCompletionSource<string> waitHandler = null;
        private ARMOrchestrationOptions options;
        private IServiceProvider serviceProvider;

        public RequestOrchestration(
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.options = options?.Value;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, RequestOrchestrationInput input)
        {
            this.waitHandler = new TaskCompletionSource<string>();
            AsyncRequestInput requestInput = options.GetRequestInput(this.serviceProvider, input);
            requestInput.EventName = eventName;
            await context.ScheduleTask<TaskResult>(typeof(AsyncRequestActivity), requestInput);
            await waitHandler.Task;
            return DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}