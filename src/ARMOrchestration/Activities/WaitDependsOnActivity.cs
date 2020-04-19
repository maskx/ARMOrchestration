using DurableTask.Core;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivity : TaskActivity<WaitDependsOnActivityInput, TaskResult>
    {
        private const string commandTemplate = @"
insert into {0}
(DeploymentId,InstanceId,ExecutionId,EventName,DependsOnName,CreateTime)
values
(@DeploymentId,@InstanceId,@ExecutionId,@EventName,@DependsOnName,GETUTCDATE())";

        private readonly string commandText;
        private ARMOrchestrationOptions options;
        private readonly ARMTemplateHelper templateHelper;
        private readonly IInfrastructure infrastructure;

        public WaitDependsOnActivity(IOptions<ARMOrchestrationOptions> options,
            ARMTemplateHelper templateHelper,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.templateHelper = templateHelper;
            this.infrastructure = infrastructure;
            this.commandText = string.Format(commandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        protected override TaskResult Execute(TaskContext context, WaitDependsOnActivityInput input)
        {
            return ExecuteAsync(context, input).Result;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, WaitDependsOnActivityInput input)
        {
            DeploymentOperation deploymentOperation = new DeploymentOperation(input.DeploymentContext, this.infrastructure, input.Resource)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = ProvisioningStage.DependsOnWaited,
                Input = DataConverter.Serialize(input)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                foreach (var item in input.DependsOn)
                {
                    Dictionary<string, object> pars = new Dictionary<string, object>()
                    {
                        {"DeploymentId",input.DeploymentContext.DeploymentId },
                        {"InstanceId",context.OrchestrationInstance.InstanceId },
                        { "ExecutionId",context.OrchestrationInstance.ExecutionId},
                        { "EventName",input.EventName},
                        { "DependsOnName",item}
                    };
                    db.AddStatement(this.commandText, pars);
                }
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}