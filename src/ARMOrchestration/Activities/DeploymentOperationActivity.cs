using DurableTask.Core;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration.Activities
{
    public class DeploymentOperationActivity : TaskActivity<DeploymentOperation, TaskResult>
    {
        public static string Name { get { return "DeploymentOperationActivity"; } }
        private readonly ARMTemplateHelper templateHelper;

        public DeploymentOperationActivity(ARMTemplateHelper templateHelper)
        {
            this.templateHelper = templateHelper;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOperation input)
        {
            templateHelper.SaveDeploymentOperation(input);
            return new TaskResult() { Code = 200 };
        }
    }
}