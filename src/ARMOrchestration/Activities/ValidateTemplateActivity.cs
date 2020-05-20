using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : AsyncTaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        public static string Name { get { return "ValidateTemplateActivity"; } }
        private readonly ARMTemplateHelper templateHelper;
        private readonly IInfrastructure infrastructure;

        public ValidateTemplateActivity(ARMTemplateHelper templateHelper, IInfrastructure infrastructure)
        {
            this.templateHelper = templateHelper;
            this.infrastructure = infrastructure;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, DeploymentOrchestrationInput input)
        {
            var r = templateHelper.ParseDeployment(input);
            TaskResult tr = null;
            if (r.Result)
            {
                tr = new TaskResult(200, DataConverter.Serialize(r.Deployment));
            }
            else
            {
                tr = new TaskResult() { Code = 400, Content = r.Message };
            }

            DeploymentOperation deploymentOperation = new DeploymentOperation(input, this.infrastructure)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = r.Result ? ProvisioningStage.ValidateTemplate : ProvisioningStage.ValidateTemplateFailed,
                Input = DataConverter.Serialize(input),
                Result = DataConverter.Serialize(tr)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            return tr;
        }
    }
}