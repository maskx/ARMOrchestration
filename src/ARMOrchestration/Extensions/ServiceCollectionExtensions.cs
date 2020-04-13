﻿using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Functions;
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
                IList<(string, string, Type)> orchList;
                if (config.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                    orchList = new List<(string, string, Type)>();
                else
                    orchList = config.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                orchList.Add(("ResourceOrchestration", "1.0", typeof(ResourceOrchestration)));
                orchList.Add(("DeploymentOrchestration", "1.0", typeof(DeploymentOrchestration)));
                orchList.Add(("WaitDependsOnOrchestration", "1.0", typeof(WaitDependsOnOrchestration)));
                orchList.Add(("RequestOrchestration", "1.0", typeof(RequestOrchestration)));
                return orchList;
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) =>
            {
                IList<(string, string, Type)> activityTypes;
                if (config.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                    activityTypes = new List<(string, string, Type)>();
                else
                    activityTypes = config.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                activityTypes.Add(("DeploymentOperationActivity", "1.0", typeof(DeploymentOperationActivity)));
                activityTypes.Add(("WaitDependsOnActivity", "1.0", typeof(WaitDependsOnActivity)));
                activityTypes.Add(("ValidateTemplateActivity", "1.0", typeof(ValidateTemplateActivity)));
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