using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorkerOptions
    {
        internal const string WaitDependsOnTable = "_WaitDependsOn";
        public string ConnectionString { get; set; }

        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; }

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public string WaitDependsOnTableName => $"[{SchemaName}].[{HubName}{WaitDependsOnTable}]";
    }
}