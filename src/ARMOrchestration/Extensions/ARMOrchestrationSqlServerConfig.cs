using DurableTask.Core.Settings;
using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationSqlServerConfig
    {
        public bool AutoCreate { get; set; } = false;
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; } = "ARM";

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";
        public bool IncludeDetails { get; set; } = false;

        public Action<ARMFunctions> ConfigARMFunctions { get; set; }
        public OrchestrationSetting OrchestrationSettings { get; set; } = new OrchestrationSetting();
        public CommunicationSetting CommunicationSettings { get; set; } = new CommunicationSetting();
        public class OrchestrationSetting
        {
            public int FetchJobCount { get; set; } = 100;
            public Func<IServiceProvider, IList<(string Name, string Version, Type Type)>> GetBuildInTaskActivities { get; set; }
            public Func<IServiceProvider, IList<(string Name, string Version, Type Type)>> GetBuildInOrchestrators { get; set; }
            public Func<IServiceProvider, IDictionary<Type, (string Version, object Instance)>> GetBuildInTaskActivitiesFromInterface { get; set; }
            public bool JumpStartEnabled { get; set; }
            public double SessionLockedSeconds { get; set; }
            public double MessageLockedSeconds { get; set; }
            public int IdleSleepMilliSeconds { get; set; }
            public TaskOrchestrationDispatcherSettings TaskOrchestrationDispatcherSettings { get; set; }
            public TaskActivityDispatcherSettings TaskActivityDispatcherSettings { get; set; }
            public TrackingDispatcherSettings TrackingDispatcherSettings { get; set; }
        }
        public class CommunicationSetting
        {
            /// <summary>
            /// Message locked time
            /// </summary>
            public double MessageLockedSeconds { get; set; } = 300;
            /// <summary>
            /// Idel time when no job fetched
            /// </summary>
            public int IdelMilliseconds { get; set; } = 10000;
            /// <summary>
            /// 外部系统请求的最大并发数
            /// </summary>
            public int MaxConcurrencyRequest { get; set; } = 100;
        }
    }
}