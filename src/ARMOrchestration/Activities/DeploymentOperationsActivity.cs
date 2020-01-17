﻿using DurableTask.Core;
using DurableTask.Core.Tracing;
using maskx.ARMOrchestration.Orchestrations;
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
        private readonly TemplateOrchestrationOptions options;
        private readonly string commandString;

        private const string commandTemplate = @"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@ExecutionId)) as [Source](InstanceId,ExecutionId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[DeploymentId],[CorrelationId],[ParentId],[Resource],[Type],[Stage],[ResourceId],[CreateTimeUtc],[UpdateTimeUtc])
	VALUES
	(@InstanceId,@ExecutionId,@DeploymentId,@CorrelationId,@ParentId,@Resource,@Type,@Stage,@ResourceId,GETUTCDATE(),GETUTCDATE())
WHEN MATCHED THEN
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=@Result;
";

        public DeploymentOperationsActivity(IOptions<TemplateOrchestrationOptions> options)
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
                { "DeploymentId",input.DeploymentId},
                { "CorrelationId",input.CorrelationId},
                { "ResourceId",input.ResourceId},
                { "Resource",input.Resource},
                { "Stage",(int)input.Stage},
                { "Type",input.Type}
            };
            if (string.IsNullOrEmpty(input.ParentId))
                pars.Add("ParentId", DBNull.Value);
            else
                pars.Add("ParentId", input.ParentId);
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