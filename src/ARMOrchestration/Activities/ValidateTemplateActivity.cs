using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : TaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        private readonly ARMTemplateHelper templateHelper;
        private readonly IInfrastructure infrastructure;

        public ValidateTemplateActivity(ARMTemplateHelper templateHelper, IInfrastructure infrastructure)
        {
            this.templateHelper = templateHelper;
            this.infrastructure = infrastructure;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOrchestrationInput input)
        {
            DeploymentOperation deploymentOperation = new DeploymentOperation(input, this.infrastructure)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = ProvisioningStage.StartProcessing,
                Input = DataConverter.Serialize(input)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            var r = templateHelper.ParseDeployment(input);
            if (r.Result)
                return new TaskResult(200, DataConverter.Serialize(r.Deployment));
            else
                return new TaskResult() { Code = 400, Content = r.Message };
        }
    }
}