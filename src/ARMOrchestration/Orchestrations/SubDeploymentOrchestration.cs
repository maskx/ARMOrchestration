using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class SubDeploymentOrchestration<T> : DeploymentOrchestration<T>
        where T : CommunicationJob, new()
    {
        public new const string Name = "SubDeploymentOrchestration";
        public SubDeploymentOrchestration(ARMTemplateHelper helper, IInfrastructure infrastructure, IServiceProvider serviceProvider)
            : base(helper, infrastructure, serviceProvider)
        {
        }
        public override async Task<TaskResult> RunTask(OrchestrationContext context, string input)
        {
            if (!context.IsReplaying)
            {
                var res = DataConverter.Deserialize<ResourceOrchestrationInput>(input);
                res.ServiceProvider = this._ServiceProvider;
                var dep = Deployment.Parse(res.Resource);
                var _ = dep.Template.Variables;
                dep.DeploymentId = context.OrchestrationInstance.InstanceId;
                helper.SaveDeploymentOperation(new DeploymentOperation(dep)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = Activities.ProvisioningStage.StartProvisioning,
                    Input = DataConverter.Serialize(dep)
                });
            }
            return await InnerRunTask(context, context.OrchestrationInstance.InstanceId);
        }
    }
}
