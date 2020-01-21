namespace maskx.ARMOrchestration
{
    public class DatabaseConfig
    {
        public bool AutoCreate { get; set; } = false;
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
}