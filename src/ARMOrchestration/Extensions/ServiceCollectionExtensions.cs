using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Workers;
using maskx.OrchestrationService.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingARMOrchestration(this IServiceCollection services, Func<IServiceProvider, ARMOrchestrationSqlServerConfig> configOption)
        {
            services.AddSingleton((sp) =>
            {
                return configOption(sp);
            });
            services.UsingOrchestration((sp) =>
            {
                var config = sp.GetService<ARMOrchestrationSqlServerConfig>();
                SqlServerConfiguration sqlServerConfiguration = new SqlServerConfiguration()
                {
                    AutoCreate = config.Database.AutoCreate,
                    ConnectionString = config.Database.ConnectionString,
                    HubName = config.Database.HubName,
                    SchemaName = config.Database.SchemaName,
                    CommunicationWorkerOptions = config.CommunicationWorkerOptions,
                    OrchestrationServiceSettings = config.OrchestrationServiceSettings
                };
                sqlServerConfiguration.OrchestrationWorkerOptions.FetchJobCount = config.OrchestrationWorkerOptions.FetchJobCount;
                sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface = config.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
                sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInOrchestrators = (sp) =>
                {
                    IList<(string, string, Type)> orchList;
                    if (config.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                        orchList = new List<(string, string, Type)>();
                    else
                        orchList = config.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                    orchList.Add((ResourceOrchestration.Name, "1.0", typeof(ResourceOrchestration)));
                    orchList.Add((DeploymentOrchestration.Name, "1.0", typeof(DeploymentOrchestration)));
                    orchList.Add((RequestOrchestration.Name, "1.0", typeof(RequestOrchestration)));
                    orchList.Add((CopyOrchestration.Name, "1.0", typeof(CopyOrchestration)));
                    return orchList;
                };
                sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) =>
                {
                    IList<(string, string, Type)> activityTypes;
                    if (config.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                        activityTypes = new List<(string, string, Type)>();
                    else
                        activityTypes = config.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                    activityTypes.Add((WaitDependsOnActivity.Name, "1.0", typeof(WaitDependsOnActivity)));
                    activityTypes.Add((AsyncRequestActivity.Name, "1.0", typeof(AsyncRequestActivity)));
                    return activityTypes;
                };
                return sqlServerConfiguration;
            });
            services.AddSingleton<ARMOrchestrationClient>();
            services.AddSingleton<ARMTemplateHelper>();
            services.AddSingleton<ARMFunctions>();
            services.AddSingleton<WaitDependsOnWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker>());
            services.AddSingleton((sp) =>
            {
                var config = sp.GetService<ARMOrchestrationSqlServerConfig>();
                var option = new ARMOrchestrationOptions
                {
                    Database = config.Database
                };
                return Options.Create(option);
            });
            return services;
        }

        public static IServiceCollection UsingARMOrchestration(this IServiceCollection services, ARMOrchestrationSqlServerConfig config)
        {
            SqlServerConfiguration sqlServerConfiguration = new SqlServerConfiguration()
            {
                AutoCreate = config.Database.AutoCreate,
                ConnectionString = config.Database.ConnectionString,
                HubName = config.Database.HubName,
                SchemaName = config.Database.SchemaName,
                CommunicationWorkerOptions = config.CommunicationWorkerOptions,
                OrchestrationServiceSettings = config.OrchestrationServiceSettings
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.FetchJobCount = config.OrchestrationWorkerOptions.FetchJobCount;
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface = config.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInOrchestrators = (sp) =>
            {
                IList<(string, string, Type)> orchList;
                if (config.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                    orchList = new List<(string, string, Type)>();
                else
                    orchList = config.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                orchList.Add((ResourceOrchestration.Name, "1.0", typeof(ResourceOrchestration)));
                orchList.Add((DeploymentOrchestration.Name, "1.0", typeof(DeploymentOrchestration)));
                orchList.Add((RequestOrchestration.Name, "1.0", typeof(RequestOrchestration)));
                orchList.Add((CopyOrchestration.Name, "1.0", typeof(CopyOrchestration)));
                return orchList;
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) =>
            {
                IList<(string, string, Type)> activityTypes;
                if (config.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                    activityTypes = new List<(string, string, Type)>();
                else
                    activityTypes = config.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                activityTypes.Add((WaitDependsOnActivity.Name, "1.0", typeof(WaitDependsOnActivity)));
                activityTypes.Add((AsyncRequestActivity.Name, "1.0", typeof(AsyncRequestActivity)));
                return activityTypes;
            };
            services.UsingOrchestration(sqlServerConfiguration);

            #region WaitDependsOnWorker

            services.AddSingleton<WaitDependsOnWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker>());

            #endregion WaitDependsOnWorker

            services.AddSingleton<ARMOrchestrationClient>();
            services.AddSingleton<ARMTemplateHelper>();
            services.AddSingleton<ARMFunctions>();
            services.Configure<ARMOrchestrationOptions>((opt) =>
            {
                opt.Database = config.Database;
            });
            return services;
        }
    }
}