using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : TaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        private ARMOrchestrationOptions ARMOptions;
        private ARMTemplateHelper templateHelper;

        public ValidateTemplateActivity(
            IOptions<ARMOrchestrationOptions> options,
            ARMTemplateHelper templateHelper)
        {
            this.ARMOptions = options?.Value;
            this.templateHelper = templateHelper;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOrchestrationInput input)
        {
            var r = templateHelper.ParseDeployment(input);
            if (r.Result)
                return new TaskResult(200, DataConverter.Serialize(r.Deployment));
            else
                return new TaskResult() { Code = 400, Content = r.Message };
        }
    }
}