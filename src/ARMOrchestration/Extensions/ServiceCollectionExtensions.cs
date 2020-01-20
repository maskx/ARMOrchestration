using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Workers;
using maskx.OrchestrationService.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace maskx.ARMOrchestration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingARMOrchestration(this IServiceCollection services, ARMOrchestrationSqlServerConfig config)
        {
            SqlServerConfiguration sqlServerConfiguration = new SqlServerConfiguration()
            {
                AutoCreate = config.AutoCreate,
                ConnectionString = config.ConnectionString,
                HubName = config.HubName,
                SchemaName = config.SchemaName,
                CommunicationWorkerOptions = config.CommunicationWorkerOptions,
                OrchestrationServiceSettings = config.OrchestrationServiceSettings,
                OrchestrationWorkerOptions = config.OrchestrationWorkerOptions
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInOrchestrators = (sp) =>
            {
                var orchList = config.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                orchList.Add(typeof(ResourceOrchestration));
                orchList.Add(typeof(TemplateOrchestration));
                orchList.Add(typeof(WaitDependsOnOrchestration));
                orchList.Add(typeof(CopyOrchestration));
                return orchList;
            };
            sqlServerConfiguration.OrchestrationWorkerOptions.GetBuildInTaskActivities = (sp) =>
            {
                var activityTypes = config.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                activityTypes.Add(typeof(DeploymentOperationsActivity));
                activityTypes.Add(typeof(WaitDependsOnActivity));
                activityTypes.Add(typeof(PrepareTemplateActivity));
                activityTypes.Add(typeof(PrepareResourceActivity));
                return activityTypes;
            };
            services.UsingOrchestration(sqlServerConfiguration);

            #region WaitDependsOnWorker

            services.AddSingleton<WaitDependsOnWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker>());

            #endregion WaitDependsOnWorker

            services.AddSingleton<ARMOrchestrationClient>();
            return services;
        }
    }
}