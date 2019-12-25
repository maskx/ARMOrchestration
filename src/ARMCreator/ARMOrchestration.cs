using DurableTask.Core;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestration : TaskOrchestration<TaskResult, string>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            ARMOrchestrationInput input = this.DataConverter.Deserialize<ARMOrchestrationInput>(arg);
            List<Task> tasks = new List<Task>();
            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("parameters", input.Parameters);
            armContext.Add("parametersdefine", input.Template.Parameters);
            armContext.Add("variabledefine", input.Template.Variables);
            armContext.Add("userDefinedFunctions", input.Template.Functions);

            foreach (var resource in input.Template.Resources)
            {
                var p = new CreateOrUpdateInput()
                {
                    Resource = resource,
                    OrchestrationContext = armContext
                };
                var t = await context.CreateSubOrchestrationInstance<string>(typeof(CreateOrUpdateOrchestration), p);
            }
            Task.WaitAll(tasks.ToArray());
            string rtv = string.Empty;
            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                rtv = ARMFunctions.GetOutputs(input.Template.Outputs, armContext);
            }
            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}