using DurableTask.Core;
using maskx.ARMOrchestration.WhatIf;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class PredictTemplateOrchestration : TaskOrchestration<TaskResult, PredictTemplateOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, PredictTemplateOrchestrationInput input)
        {
            WhatIfOperationResult result = new WhatIfOperationResult();

            return new TaskResult() { Code = 200, Content = DataConverter.Serialize(result) };
        }
    }
}