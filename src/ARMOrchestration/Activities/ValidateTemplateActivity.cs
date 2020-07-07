using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System;

namespace maskx.ARMOrchestration.Activities
{
    public class ValidateTemplateActivity : TaskActivity<DeploymentOrchestrationInput, TaskResult>
    {
        public const string Name = "ValidateTemplateActivity";
        private readonly ARMTemplateHelper templateHelper;
        private readonly IInfrastructure infrastructure;

        public ValidateTemplateActivity(ARMTemplateHelper templateHelper, IInfrastructure infrastructure)
        {
            this.templateHelper = templateHelper;
            this.infrastructure = infrastructure;
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOrchestrationInput input)
        {
            TaskResult tr;
            try
            {
                var Deployment = DeploymentOrchestrationInput.Validate(input, templateHelper.ARMfunctions, infrastructure);
                tr = new TaskResult(200, DataConverter.Serialize(Deployment));
            }
            catch (Exception ex)
            {
                tr = new TaskResult() { Code = 400, Content = ex.Message };
            }
            DeploymentOperation deploymentOperation = new DeploymentOperation(input, this.infrastructure)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = tr.Code == 200 ? ProvisioningStage.ValidateTemplate : ProvisioningStage.ValidateTemplateFailed,
                Input = DataConverter.Serialize(input),
                Result = DataConverter.Serialize(tr)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            return tr;
        }
    }
}