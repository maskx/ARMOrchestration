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
            var r = templateHelper.ValidateTemplate(input);
            if (r.Result)
                return new TaskResult(200, DataConverter.Serialize(new DeploymentContext()
                {
                    CorrelationId = input.CorrelationId,
                    RootId = context.OrchestrationInstance.InstanceId,
                    DeploymentId = string.IsNullOrEmpty(input.DeploymentId) ? context.OrchestrationInstance.InstanceId : input.DeploymentId,
                    DeploymentName = input.Name,
                    Mode = input.Mode,
                    ResourceGroup = input.ResourceGroup,
                    SubscriptionId = input.SubscriptionId,
                    TenantId = input.TenantId,
                    Parameters = input.Parameters,
                    Template = r.Template
                }));
            else
                return new TaskResult() { Code = 400, Content = r.Message };
        }
    }
}