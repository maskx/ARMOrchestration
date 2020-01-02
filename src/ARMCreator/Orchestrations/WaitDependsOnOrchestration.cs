using DurableTask.Core;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator.Activity
{
    public class WaitDependsOnOrchestration : TaskOrchestration<TaskResult, (string dependsOn, Dictionary<string, object> context)>
    {
        private const string eventName = "WaitDependsOn";
        private TaskCompletionSource<string> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, (string dependsOn, Dictionary<string, object> context) input)
        {
            using var jsonDoc = JsonDocument.Parse(input.dependsOn);
            return new TaskResult();
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}