using maskx.OrchestrationService.Activity;
using System;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestrationOptions
    {
        public BuitinServiceTypes BuitinServiceTypes { get; set; }

        /// <summary>
        /// Idel time when no dependsOn resource completed
        /// </summary>
        public int DependsOnIdelMilliseconds { get; set; } = 500;

        public DatabaseConfig Database { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckPolicyRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCreateResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitResourceRequestInput { get; set; }
        public Func<string, string, AsyncRequestInput> GetCheckLockRequestInput { get; set; }

        public class DatabaseConfig
        {
            internal const string WaitDependsOnTable = "_WaitDependsOn";
            internal const string DeploymentOperationsTable = "_DeploymentOperations";

            public string ConnectionString { get; set; }

            /// Gets or sets the hub name for the database instance store.
            /// </summary>
            public string HubName { get; set; }

            /// <summary>
            /// Gets or sets the schema name to which the tables will be added.
            /// </summary>
            public string SchemaName { get; set; } = "dbo";

            public string WaitDependsOnTableName => $"[{SchemaName}].[{HubName}{WaitDependsOnTable}]";
            public string DeploymentOperationsTableName => $"[{SchemaName}].[{HubName}{DeploymentOperationsTable}]";
        }
    }

    public class BuitinServiceTypes
    {
        public string ResourceGroup { get; set; }
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
    }
}