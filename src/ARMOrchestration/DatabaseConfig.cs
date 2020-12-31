namespace maskx.ARMOrchestration
{
    public class DatabaseConfig
    {
        public bool AutoCreate { get; set; } = false;
        internal const string WaitDependsOnTable = "WaitDependsOn";
        internal const string DeploymentOperationsTable = "DeploymentOperations";
        internal const string DeploymentOperationHistoryTable = "DeploymentOperationHistory";

        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; } = "ARM";

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public string WaitDependsOnTableName => $"[{SchemaName}].[{HubName}_{WaitDependsOnTable}]";
        public string DeploymentOperationsTableName => $"[{SchemaName}].[{HubName}_{DeploymentOperationsTable}]";
        public string DeploymentOperationHistoryTableName => $"[{SchemaName}].[{HubName}_{DeploymentOperationHistoryTable}]";
        internal string RetrySPName => $"[{SchemaName}].[{HubName}_PrepareRetry]";
    }
}