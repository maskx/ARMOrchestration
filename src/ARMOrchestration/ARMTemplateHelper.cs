using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        public readonly ARMFunctions ARMfunctions;
        public readonly IInfrastructure infrastructure;

        private readonly string _saveDeploymentOperationCommandString;

        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions functions,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.ARMfunctions = functions;
            this.infrastructure = infrastructure;

            this._saveDeploymentOperationCommandString = string.Format(@"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@ExecutionId)) as [Source](InstanceId,ExecutionId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Result],[Comments],[CreateByUserId],[LastRunUserId])
	VALUES
	(@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,@Input,@Result,@Comments,@CreateByUserId,@LastRunUserId)
WHEN MATCHED THEN
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(cast(@Result AS NVARCHAR(MAX)),[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(cast(@Input AS NVARCHAR(MAX)),[Input]);
", this.options.Database.DeploymentOperationsTableName);
        }

        // TODO: when this is a async task, in orchestration await this will make orchestration cannot be completed, need investigation
        public void SaveDeploymentOperation(DeploymentOperation deploymentOperation)
        {
            TraceActivityEventSource.Log.TraceEvent(
                TraceEventType.Information,
                "DeploymentOperationsActivity",
                deploymentOperation.InstanceId,
                deploymentOperation.ExecutionId,
                $"{deploymentOperation.Type}:{deploymentOperation.ResourceId}:{(int)deploymentOperation.Stage}",
                deploymentOperation.Input,
                deploymentOperation.Stage.ToString());

            using var db = new DbAccess(this.options.Database.ConnectionString);
            db.AddStatement(this._saveDeploymentOperationCommandString, deploymentOperation);
            db.ExecuteNonQueryAsync().Wait();
        }
    }
}