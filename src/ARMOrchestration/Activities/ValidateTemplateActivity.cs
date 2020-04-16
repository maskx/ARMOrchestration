using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : TaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        private ARMTemplateHelper templateHelper;

        public ValidateTemplateActivity(ARMTemplateHelper templateHelper)
        {
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