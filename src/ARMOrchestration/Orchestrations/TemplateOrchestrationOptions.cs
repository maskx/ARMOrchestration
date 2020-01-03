using maskx.OrchestrationService.Activity;
using System;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestrationOptions
    {
        public BuitinServiceTypes BuitinServiceTypes { get; set; }

        public DatabaseConfig Database { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckPolicyRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetBeginCreateResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCreateResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitResourceRequestInput { get; set; }
        public Func<string, string, AsyncRequestInput> GetLockCheckRequestInput { get; set; }

        public class DatabaseConfig
        {
            internal const string WaitDependsOnTable = "_WaitDependsOn";
            internal const string DeploymentDetailTable = "_DeploymentDetail";

            public string ConnectionString { get; set; }

            /// Gets or sets the hub name for the database instance store.
            /// </summary>
            public string HubName { get; set; }

            /// <summary>
            /// Gets or sets the schema name to which the tables will be added.
            /// </summary>
            public string SchemaName { get; set; } = "dbo";

            public string WaitDependsOnTableName => $"[{SchemaName}].[{HubName}{WaitDependsOnTable}]";
            public string DeploymentDetailTableName => $"[{SchemaName}].[{HubName}{DeploymentDetailTable}]";
        }
    }

    public class BuitinServiceTypes
    {
        public string ResourceGroup { get; set; }
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
    }
}