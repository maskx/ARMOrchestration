﻿using DurableTask.Core;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivity : AsyncTaskActivity<WaitDependsOnActivityInput, TaskResult>
    {
        public static string Name { get { return "WaitDependsOnActivity"; } }

        private const string commandTemplate = @"
insert into {0}
(RootId,DeploymentId,InstanceId,ExecutionId,EventName,DependsOnName,CreateTime)
values
(@RootId,@DeploymentId,@InstanceId,@ExecutionId,@EventName,@DependsOnName,GETUTCDATE())";

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

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, WaitDependsOnActivityInput input)
        {
            DeploymentOperation deploymentOperation = new DeploymentOperation(input.DeploymentContext, this.infrastructure, input.Resource)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = input.ProvisioningStage,
                Input = DataConverter.Serialize(input)
            };
            templateHelper.SaveDeploymentOperation(deploymentOperation);
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                foreach (var item in input.DependsOn)
                {
                    db.AddStatement(this.commandText, new
                    {
                        RootId=input.DeploymentContext.RootId,
                        DeploymentId = input.DeploymentContext.DeploymentId,
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        EventName = input.ProvisioningStage.ToString(),
                        DependsOnName = item
                    });
                }
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}