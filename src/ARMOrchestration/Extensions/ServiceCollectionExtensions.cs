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
                    Database = config.Database
                };
                return Options.Create(option);
            });
            services.UsingSQLServerOrchestration((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new SqlServerOrchestrationConfiguration()
                {
                    ConnectionString = config.Database.ConnectionString,
                    HubName = config.Database.HubName,
                    SchemaName = config.Database.SchemaName
                };
            });
            services.UsingOrchestrationWorker((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new OrchestrationWorkerOptions()
                {
                    AutoCreate = config.Database.AutoCreate,
                    FetchJobCount = config.OrchestrationWorkerOptions.FetchJobCount,
                    GetBuildInOrchestrators = config.OrchestrationWorkerOptions.GetBuildInOrchestrators,
                    GetBuildInTaskActivities = config.OrchestrationWorkerOptions.GetBuildInTaskActivities,
                    GetBuildInTaskActivitiesFromInterface = config.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface,
                    IncludeDetails = config.IncludeDetails
                };
            });

            services.UsingCommunicationWorker<T>((sp) =>
            {
                if (config == null)
                    config = configOption(sp);
                return new CommunicationWorkerOptions()
                {
                    AutoCreate = config.Database.AutoCreate,
                    ConnectionString = config.Database.ConnectionString,
                    HubName = config.Database.HubName,
                    SchemaName = config.Database.SchemaName,
                    IdelMilliseconds = config.CommunicationWorkerOptions.IdelMilliseconds,
                    MaxConcurrencyRequest = config.CommunicationWorkerOptions.MaxConcurrencyRequest,
                    MessageLockedSeconds = config.CommunicationWorkerOptions.MessageLockedSeconds
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
                    ConnectionString = config.Database.ConnectionString,
                    HubName = config.Database.HubName,
                    SchemaName = config.Database.SchemaName
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
                    Database = config.Database
                };
                return Options.Create(option);
            });
            services.TryAddSingleton<ARMOrchestrationClient<T>>();
            return services;
        }
    }
}