using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration.Activities
{
    public class PrepareDeploymentActivity : TaskActivity<TemplateOrchestrationInput, TaskResult>
    {
        protected override TaskResult Execute(TaskContext context, TemplateOrchestrationInput input)
        {
            var r = Helper.ValidateTemplate(input);
            if (r.Result)
                return new TaskResult(200, DataConverter.Serialize(new DeploymentContext()
                {
                    CorrelationId = input.CorrelationId,
                    DeploymentId = input.DeploymentId,
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