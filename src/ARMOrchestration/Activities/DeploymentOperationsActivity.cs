using DurableTask.Core;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class DeploymentOperationsActivity : TaskActivity<DeploymentOperationsActivityInput, TaskResult>
    {
        private readonly ARMOrchestrationOptions options;
        private readonly string commandString;

        private const string commandTemplate = @"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@ExecutionId)) as [Source](InstanceId,ExecutionId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input])
	VALUES
	(@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,@Input)
WHEN MATCHED THEN
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=@Result;
";

        public DeploymentOperationsActivity(IOptions<ARMOrchestrationOptions> options)
        {
            this.options = options.Value;
            this.commandString = string.Format(commandTemplate, this.options.Database.DeploymentOperationsTableName);
        }

        protected override TaskResult Execute(TaskContext context, DeploymentOperationsActivityInput input)
        {
            return ExecuteAsync(context, input).Result;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, DeploymentOperationsActivityInput input)
        {
            TraceActivityEventSource.Log.TraceEvent(
                TraceEventType.Information,
                "DeploymentOperationsActivity",
                context.OrchestrationInstance.InstanceId,
                context.OrchestrationInstance.ExecutionId,
                $"{input.ResourceId}-{input.Stage}",
                DataConverter.Serialize(input),
                input.Stage.ToString());

            Dictionary<string, object> pars = new Dictionary<string, object>() {
                { "InstanceId",input.InstanceId },
                { "ExecutionId",input.ExecutionId},
                { "GroupId",input.DeploymentContext.GroupId },
                { "GroupType",input.DeploymentContext.GroupType },
                { "HierarchyId",input.DeploymentContext.HierarchyId },
                { "RootId",input.DeploymentContext.RootId },
                { "DeploymentId",input.DeploymentContext.DeploymentId},
                { "CorrelationId",input.DeploymentContext.CorrelationId},
                { "ResourceId",input.ResourceId},
                { "Name",input.Name},
                { "Type",input.Type},
                { "Stage",(int)input.Stage},
            };
            if (string.IsNullOrEmpty(input.DeploymentContext.SubscriptionId))
                pars.Add("SubscriptionId", DBNull.Value);
            else
                pars.Add("SubscriptionId", input.DeploymentContext.SubscriptionId);
            if (string.IsNullOrEmpty(input.DeploymentContext.ManagementGroupId))
                pars.Add("ManagementGroupId", DBNull.Value);
            else
                pars.Add("ManagementGroupId", input.DeploymentContext.ManagementGroupId);
            if (string.IsNullOrEmpty(input.ParentId))
                pars.Add("ParentResourceId", DBNull.Value);
            else
                pars.Add("ParentResourceId", input.ParentId);
            if (string.IsNullOrWhiteSpace(input.Input))
                pars.Add("Input", DBNull.Value);
            else
                pars.Add("Input", input.Input);
            if (string.IsNullOrEmpty(input.Result))
                pars.Add("Result", DBNull.Value);
            else
                pars.Add("Result", input.Result);
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(this.commandString, pars);
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}