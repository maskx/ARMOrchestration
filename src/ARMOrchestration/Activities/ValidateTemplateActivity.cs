using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : TaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        public static string Name { get { return "ValidateTemplateActivity"; } }
        private readonly ARMTemplateHelper templateHelper;
        private readonly IInfrastructure infrastructure;

        public ValidateTemplateActivity(ARMTemplateHelper templateHelper, IInfrastructure infrastructure)
        {
            this.templateHelper = templateHelper;
            this.infrastructure = infrastructure;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOrchestrationInput input)
        {
            var (Result, Message, Deployment) = templateHelper.ParseDeployment(input);
            TaskResult tr;
            if (Result)
            {
                tr = new TaskResult(200, DataConverter.Serialize(Deployment));
            }
            else
            {
                tr = new TaskResult() { Code = 400, Content = Message };
            }

            DeploymentOperation deploymentOperation = new DeploymentOperation(input, this.infrastructure)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = Result ? ProvisioningStage.ValidateTemplate : ProvisioningStage.ValidateTemplateFailed,
                Input = DataConverter.Serialize(input),
                Result = DataConverter.Serialize(tr)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            return tr;
        }
    }
}