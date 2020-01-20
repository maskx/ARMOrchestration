using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Workers;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingARMOrchestration(this IServiceCollection services, ARMOrchestrationConfig options)
        {
            services.AddSingleton<IOrchestrationService>(
                new SQLServerOrchestrationService(
                       options.ConnectionString,
                       options.HubName,
                       new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
                       {
                           HubName = options.HubName,
                           ConnectionString = options.ConnectionString
                       }),
                       options.OrchestrationServiceSettings));
            services.AddSingleton<IOrchestrationServiceClient>(
                new SQLServerOrchestrationService(
                       options.ConnectionString,
                       options.HubName,
                       new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
                       {
                           HubName = options.HubName,
                           ConnectionString = options.ConnectionString
                       }),
                       options.OrchestrationServiceSettings));

            #region OrchestrationWorker

            services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
            {
                OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                orchestrationCreatorFactory.RegistCreator("DICreator", typeof(DICreator<TaskOrchestration>));
                orchestrationCreatorFactory.RegistCreator("DefaultObjectCreator", typeof(DefaultObjectCreator<TaskOrchestration>));
                return orchestrationCreatorFactory;
            });

            var orchestrationTypes = new List<Type>();

            var activityTypes = new List<Type>();

            orchestrationTypes.Add(typeof(AsyncRequestOrchestration));
            orchestrationTypes.Add(typeof(ResourceOrchestration));
            orchestrationTypes.Add(typeof(TemplateOrchestration));
            orchestrationTypes.Add(typeof(WaitDependsOnOrchestration));
            orchestrationTypes.Add(typeof(CopyOrchestration));
            activityTypes.Add(typeof(AsyncRequestActivity));
            activityTypes.Add(typeof(HttpRequestActivity));
            activityTypes.Add(typeof(DeploymentOperationsActivity));
            activityTypes.Add(typeof(WaitDependsOnActivity));
            activityTypes.Add(typeof(PrepareTemplateActivity));
            activityTypes.Add(typeof(ValidateTemplateActivity));
            services.Configure<OrchestrationWorkerOptions>(opt =>
            {
                opt.GetBuildInOrchestrators = (sp) => orchestrationTypes;
                opt.GetBuildInTaskActivities = (sp) => activityTypes;
            });

            services.AddSingleton<OrchestrationWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());

            #endregion OrchestrationWorker

            #region CommunicationWorker

            services.Configure<CommunicationWorkerOptions>((opt) =>
            {
                opt.ConnectionString = options.ConnectionString;
                opt.HubName = opt.HubName;
            });
            services.AddHostedService<CommunicationWorker>();

            #endregion CommunicationWorker

            #region WaitDependsOnWorker

            services.AddSingleton<WaitDependsOnWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<WaitDependsOnWorker>());

            #endregion WaitDependsOnWorker

            return services;
        }
    }
}