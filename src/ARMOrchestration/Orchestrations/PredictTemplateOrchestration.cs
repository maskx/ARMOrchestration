using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.WhatIf;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class PredictTemplateOrchestration : TaskOrchestration<TaskResult, PredictTemplateOrchestrationInput>
    {
        private readonly ARMTemplateHelper helper;

        public PredictTemplateOrchestration(ARMTemplateHelper helper)
        {
            this.helper = helper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, PredictTemplateOrchestrationInput input)
        {
            WhatIfOperationResult result = helper.WhatIf(input);

            return new TaskResult() { Code = 200, Content = DataConverter.Serialize(result) };
        }
    }
}