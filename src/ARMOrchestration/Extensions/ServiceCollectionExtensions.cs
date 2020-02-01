using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Workers;
using maskx.OrchestrationService.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public static class ServiceCollectionExtensions
    {
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
                IList<Type> orchList;
                if (config.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                    orchList = new List<Type>();
                else
                    orchList = config.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                orchList.Add(typeof(ResourceOrchestration));
                orchList.Add(typeof(DeploymentOrchestration));
                orchList.Add(typeof(WaitDependsOnOrchestration));
                orchList.Add(typeof(CopyOrchestration));
                orchList.Add(typeof(RequestOrchestration));
                return orchList;
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) =>
            {
                IList<Type> activityTypes;
                if (config.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                    activityTypes = new List<Type>();
                else
                    activityTypes = config.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                activityTypes.Add(typeof(DeploymentOperationsActivity));
                activityTypes.Add(typeof(WaitDependsOnActivity));

                activityTypes.Add(typeof(ValidateTemplateActivity));
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
                opt.GetRequestInput = config.GetRequestInput;
                opt.ExtensionResources = config.ExtensionResources;
            });
            return services;
        }
    }
}