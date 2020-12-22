using DurableTask.Core;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivity : AsyncTaskActivity<WaitDependsOnActivityInput, TaskResult>
    {
        public const string Name = "WaitDependsOnActivity";

        private const string commandTemplate = @"
insert into {0}
(RootId,DeploymentId,InstanceId,ExecutionId,EventName,DependsOnName,CreateTime)
values
(@RootId,@DeploymentId,@InstanceId,@ExecutionId,@EventName,@DependsOnName,GETUTCDATE())";

        private readonly string commandText;
        private readonly ARMOrchestrationOptions options;
        private readonly ARMTemplateHelper templateHelper;

        public WaitDependsOnActivity(IOptions<ARMOrchestrationOptions> options, ARMTemplateHelper templateHelper)
        {
            this.options = options?.Value;
            this.templateHelper = templateHelper;
            this.commandText = string.Format(commandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, WaitDependsOnActivityInput input)
        {
            templateHelper.SaveDeploymentOperation(new DeploymentOperation()
            {
                DeploymentId = input.DeploymentId,
                InstanceId = input.InstanceId,
                ExecutionId=input.ExecutionId,
                Stage=ProvisioningStage.DependsOnWaited
            });
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                foreach (var item in input.DependsOn)
                {
                    db.AddStatement(this.commandText, new
                    {
                        input.RootId,
                        input.DeploymentId,
                        context.OrchestrationInstance.InstanceId,
                        context.OrchestrationInstance.ExecutionId,
                        EventName = ProvisioningStage.DependsOnWaited.ToString(),
                        DependsOnName = item
                    });
                }
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}