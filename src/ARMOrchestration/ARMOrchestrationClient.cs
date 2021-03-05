using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationClient<T> where T : CommunicationJob, new()
    {
        private readonly OrchestrationWorkerClient _OrchestrationWorkerClient;
        private readonly DataConverter _DataConverter = new JsonDataConverter();
        private readonly ARMOrchestrationOptions _Options;
        private readonly IServiceProvider _ServiceProvider;
        private readonly ARMTemplateHelper _Helper;
        private readonly IInfrastructure _Infrastructure;
        private readonly ILoggerFactory _LoggerFactory;
        public ARMOrchestrationClient(
            OrchestrationWorkerClient orchestrationWorkerClient,
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider,
            ARMTemplateHelper helper,
            IInfrastructure infrastructure,
            ILoggerFactory loggerFactory)
        {
            this._LoggerFactory = loggerFactory;
            this._ServiceProvider = serviceProvider;
            this._Infrastructure = infrastructure;
            this._OrchestrationWorkerClient = orchestrationWorkerClient;
            this._Options = options?.Value;
            this._Helper = helper;
        }

        public async Task<DeploymentOperation> Run(Deployment args)
        {
            if (string.IsNullOrEmpty(args.DeploymentId))
                throw new ArgumentNullException("DeploymentId");
            if (string.IsNullOrEmpty(args.ApiVersion))
                throw new ArgumentNullException("ApiVersion");
            if (string.IsNullOrEmpty(args.CorrelationId))
                throw new ArgumentNullException("CorrelationId");
            if (string.IsNullOrEmpty(args.Name))
                throw new ArgumentNullException("DeploymentName");
            if (string.IsNullOrEmpty(args.GroupId))
                throw new ArgumentNullException("GroupId");
            if (string.IsNullOrEmpty(args.GroupType))
                throw new ArgumentNullException("GroupType");
            if (string.IsNullOrEmpty(args.HierarchyId))
                throw new ArgumentNullException("HierarchyId");
            if (string.IsNullOrEmpty(args.TenantId))
                throw new ArgumentNullException("TenantId");
            if (string.IsNullOrEmpty(args.SubscriptionId) && string.IsNullOrEmpty(args.ManagementGroupId))
                throw new ArgumentException("SubscriptionId and ManagementGroupId must set one");
            if (!string.IsNullOrEmpty(args.SubscriptionId) && !string.IsNullOrEmpty(args.ManagementGroupId))
                throw new ArgumentException("SubscriptionId and ManagementGroupId only one can be set value");
            if (string.IsNullOrEmpty(args.CreateByUserId))
                throw new ArgumentNullException("CreateByUserId");
            if (string.IsNullOrEmpty(args.RootId)) args.RootId = args.DeploymentId;
            if (string.IsNullOrEmpty(args.LastRunUserId)) args.LastRunUserId = args.CreateByUserId;
            if (args.ServiceProvider == null) args.ServiceProvider = _ServiceProvider;
            var (r, s) = args.Validate();
            if (!r)
                throw new Exception(s);
            var deploymentOperation = await _Helper.CreatDeploymentOperation(new DeploymentOperation(args.DeploymentId, args)
            {
                RootId = string.IsNullOrEmpty(args.ParentId) ? args.DeploymentId : args.ParentId,
                InstanceId = args.DeploymentId,
                Stage = ProvisioningStage.Pending,
                Input = _DataConverter.Serialize(args)
            });
            var instance = await _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = args.DeploymentId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = DeploymentOrchestration<T>.Name,
                    Version = args.ApiVersion
                },
                Input = args.DeploymentId
            });
            deploymentOperation.ExecutionId = instance.ExecutionId;
            return deploymentOperation;
        }
        public async Task<(int Result, string Message)> Retry(string deploymentOperationId, string correlationId, string userId)
        {
            var op = await GetDeploymentOperationAsync(deploymentOperationId);
            if (op.Stage != ProvisioningStage.Failed)
                return (400, $"stage[{op.Stage}] cannot be retry");
            if (op.Type == _Infrastructure.BuiltinServiceTypes.Deployments)
                return await RetryDeployment(op, correlationId, userId);
            if (op.Type == $"{_Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                return await RetryCopy(op, correlationId, userId);
            return await RetryResource(op, correlationId, userId);
        }
        private async Task<(int Result, string Message)> InitRetry(string deploymentOPerationId, string correlationId, string lastRunUserId, string input)
        {
            int result = 400;
            string message = $"cannot find DeploymentOperation with Id:{deploymentOPerationId}";
            using var db = new SQLServerAccess(this._Options.Database.ConnectionString, _LoggerFactory);
            await db.ExecuteStoredProcedureASync(this._Options.Database.InitRetrySPName,
                (reader, index) =>
                {
                    var effected = reader.GetInt32(0);
                    var stage = (ProvisioningStage)(int)reader["Stage"];
                    var correlation = reader["CorrelationId"].ToString();
                    var userId = reader["LastRunUserId"].ToString();
                    if (effected == 1)
                    {
                        result = 201;
                        message = "Successed";
                    }
                    else
                    {
                        if (userId == lastRunUserId && correlation == correlationId)
                        {
                            result = 202;
                            message = $"DeploymentOperation[{deploymentOPerationId}] already in {stage} stage";
                        }
                        else
                        {
                            result = 400;
                            message = $"DeploymentOperation[{ deploymentOPerationId}] already in { stage}stage;Last Run User is [{userId}]";
                        }
                    }
                },
                new
                {
                    Id = deploymentOPerationId,
                    CorrelationId = correlationId,
                    LastRunUserId = lastRunUserId,
                    Input = input
                });
            return (result, message);
        }
        public async Task<(int Result, string Message)> RetryCopy(DeploymentOperation op, string correlationId, string userId)
        {
            var input = new ResourceOrchestrationInput()
            {
                IsRetry = true,
                LastRunUserId = userId,
                DeploymentOperationId = op.Id,
                ResourceId=op.ResourceId,
                DeploymentId=op.DeploymentId
            };
            var r = await InitRetry(op.Id, correlationId, userId, null);
            if (r.Result != 201)
                return r;
            await _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                Orchestration = new OrchestrationSetting()
                {
                    Name = CopyOrchestration<T>.Name,
                    Version = op.ApiVersion
                },
                Input = input
            });
            return r;
        }
        public async Task<(int Result, string Message)> RetryResource(DeploymentOperation op, string correlationId, string userId)
        {
            var input = new ResourceOrchestrationInput()
            {
                IsRetry = true,
                LastRunUserId = userId,
                DeploymentOperationId = op.Id,
                ResourceId=op.ResourceId,
                DeploymentId=op.DeploymentId
            };
            var r = await InitRetry(op.Id, correlationId, userId, null);
            if (r.Result != 201)
                return r;
            await _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                Orchestration = new OrchestrationSetting()
                {
                    Name = ResourceOrchestration<T>.Name,
                    Version = op.ApiVersion
                },
                Input = input
            });
            return r;
        }
        public async Task<(int Result, string Message)> RetryDeployment(DeploymentOperation op, string correlationId, string userId)
        {
            var dep = _DataConverter.Deserialize<Deployment>(op.Input);
            dep.ServiceProvider = this._ServiceProvider;
            dep.IsRetry = true;
            dep.LastRunUserId = userId;
            var r = await InitRetry(op.Id, correlationId, userId, _DataConverter.Serialize(dep));
            if (r.Result != 201)
                return r;
            await _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Input = op.Id,
                Orchestration = new OrchestrationSetting()
                {
                    Name = DeploymentOrchestration<T>.Name,
                    Version = op.ApiVersion
                }
            });
            return r;
        }

        /// <summary>
        /// get the resources provisioned in this deployment, not include the nest deployment
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        public async Task<List<DeploymentOperation>> GetDeploymentOperationListAsync(string deploymentId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new SQLServerAccess(this._Options.Database.ConnectionString, _LoggerFactory))
            {
                db.AddStatement($"select * from { this._Options.Database.DeploymentOperationsTableName} where deploymentId=@deploymentId",
                    new Dictionary<string, object>() {
                        { "deploymentId",deploymentId}
                    });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rs.Add(new DeploymentOperation(reader["Id"].ToString())
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"].ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"].ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString(),
                        CreateTimeUtc = (DateTime)reader["CreateTimeUtc"],
                        UpdateTimeUtc = (DateTime)reader["UpdateTimeUtc"],
                        ApiVersion = reader["ApiVersion"].ToString(),
                        Comments = reader["Comments"].ToString()
                    });
                });
            }
            return rs;
        }

        /// <summary>
        /// get the resources provisioned in this deployment, include the nest deployment
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        public async Task<List<DeploymentOperation>> GetAllResourceListAsync(string rootId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new SQLServerAccess(this._Options.Database.ConnectionString, _LoggerFactory))
            {
                db.AddStatement($"select * from {this._Options.Database.DeploymentOperationsTableName} where RootId=@RootId",
                    new Dictionary<string, object>() {
                        { "RootId",rootId}
                    });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rs.Add(new DeploymentOperation(reader["Id"].ToString())
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"].ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"].ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString()
                    });
                });
            }
            return rs;
        }

        public async Task<DeploymentOperation> GetDeploymentOperationAsync(string deploymentOperationId)
        {
            DeploymentOperation deployment = null;
            using (var db = new SQLServerAccess(this._Options.Database.ConnectionString, _LoggerFactory))
            {
                db.AddStatement($"select * from {this._Options.Database.DeploymentOperationsTableName} where Id=@Id", new { Id = deploymentOperationId });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    deployment = new DeploymentOperation(reader["Id"].ToString())
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"].ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"].ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString(),
                        ApiVersion = reader["ApiVersion"].ToString()
                    };
                });
            }
            return deployment;
        }

    }
}