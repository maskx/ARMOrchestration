using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Workers;
using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace maskx.ARMOrchestration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingARMOrchestration<T>(this IServiceCollection services, Func<IServiceProvider, ARMOrchestrationSqlServerConfig> configOption)
        where T : CommunicationJob, new()
        {
            ARMOrchestrationSqlServerConfig config = null;
            services.TryAddSingleton((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                var option = new ARMOrchestrationOptions
                {
                    Database = new DatabaseConfig()
                    {
                        AutoCreate = config.AutoCreate,
                        ConnectionString = config.ConnectionString,
                        HubName = config.HubName,
                        SchemaName = config.SchemaName
                    }
                };
                return Options.Create(option);
            });
            services.UsingSQLServerOrchestration((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new SqlServerOrchestrationConfiguration()
                {
                    ConnectionString = config.ConnectionString,
                    HubName = config.HubName,
                    SchemaName = config.SchemaName
                };
            });
            services.UsingOrchestrationWorker((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new OrchestrationWorkerOptions()
                {
                    AutoCreate = config.AutoCreate,
                    FetchJobCount = config.OrchestrationSettings.FetchJobCount,
                    GetBuildInOrchestrators = config.OrchestrationSettings.GetBuildInOrchestrators,
                    GetBuildInTaskActivities = config.OrchestrationSettings.GetBuildInTaskActivities,
                    GetBuildInTaskActivitiesFromInterface = config.OrchestrationSettings.GetBuildInTaskActivitiesFromInterface,
                    IncludeDetails = config.IncludeDetails
                };
            });

            services.UsingCommunicationWorker<T>((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new CommunicationWorkerOptions()
                {
                    AutoCreate = config.AutoCreate,
                    ConnectionString = config.ConnectionString,
                    HubName = config.HubName,
                    SchemaName = config.SchemaName,
                    IdelMilliseconds = config.CommunicationSettings.IdelMilliseconds,
                    MaxConcurrencyRequest = config.CommunicationSettings.MaxConcurrencyRequest,
                    MessageLockedSeconds = config.CommunicationSettings.MessageLockedSeconds
                };
            });
            services.TryAddSingleton<ARMOrchestrationClient<T>>();
            services.TryAddSingleton<ARMTemplateHelper>();
            services.TryAddSingleton<ARMFunctions>((sp) =>
            {
                var options = sp.GetService<IOptions<ARMOrchestrationOptions>>();
                var infra = sp.GetService<IInfrastructure>();
                if (config == null)
                    config = configOption(sp);
                var func = new ARMFunctions(options, sp, infra);
                config.ConfigARMFunctions?.Invoke(func);
                return func;
            });
            services.TryAddSingleton<WaitDependsOnWorker<T>>();
            services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker<T>>());

            return services;
        }
        public static IServiceCollection UsingARMOrhcestrationClient<T>(this IServiceCollection services, Func<IServiceProvider, ARMOrchestrationSqlServerConfig> configOption)
            where T : CommunicationJob, new()
        {
            ARMOrchestrationSqlServerConfig config = null;
            services.UsingSQLServerOrchestration((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new SqlServerOrchestrationConfiguration()
                {
                    ConnectionString = config.ConnectionString,
                    HubName = config.HubName,
                    SchemaName = config.SchemaName
                };
            });
            services.TryAddSingleton<OrchestrationWorkerClient>();
            services.TryAddSingleton<ARMTemplateHelper>();
            services.TryAddSingleton((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                var option = new ARMOrchestrationOptions
                {
                    Database = new DatabaseConfig()
                    {
                        AutoCreate = config.AutoCreate,
                        ConnectionString = config.ConnectionString,
                        HubName = config.HubName,
                        SchemaName = config.SchemaName
                    }
                };
                return Options.Create(option);
            });
            services.TryAddSingleton<ARMOrchestrationClient<T>>();
            return services;
        }
    }
}