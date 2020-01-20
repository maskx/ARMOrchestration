using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public delegate AsyncRequestInput GetRequestInput(IServiceProvider serviceProvider, DeploymentContext context, Resource resource, string name, string property);

    public delegate TaskResult ListFunction(string resourceId, string apiVersion, string functionValues = "", string value = "");

    public class BuitinServiceTypes
    {
        public string ResourceGroup { get; set; } = "Microsoft.Resources/resourceGroups";
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
    }

    public class DatabaseConfig
    {
        internal const string WaitDependsOnTable = "_WaitDependsOn";
        internal const string DeploymentOperationsTable = "_DeploymentOperations";

        public string ConnectionString { get; set; }

        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; } = "ARM";

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public string WaitDependsOnTableName => $"[{SchemaName}].[{HubName}{WaitDependsOnTable}]";
        public string DeploymentOperationsTableName => $"[{SchemaName}].[{HubName}{DeploymentOperationsTable}]";
    }

    public class ARMOrchestrationOptions
    {
        /// <summary>
        /// Idel time when no dependsOn resource completed
        /// </summary>
        public int DependsOnIdelMilliseconds { get; set; } = 500;

        public DatabaseConfig Database { get; set; }
        public ListFunction ListFunction { get; set; }
        public GetRequestInput GetRequestInput { get; set; }
        public List<string> ExtensionResources { get; set; }
        public BuitinServiceTypes BuitinServiceTypes { get; set; } = new BuitinServiceTypes();
    }
}