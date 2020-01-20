using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationClient
    {
        private readonly OrchestrationWorkerClient orchestrationWorkerClient;
        private readonly string TemplateOrchestrationUri = typeof(TemplateOrchestration).FullName + "_";
        private readonly DataConverter DataConverter = new JsonDataConverter();

        public ARMOrchestrationClient(OrchestrationWorkerClient orchestrationWorkerClient)
        {
            this.orchestrationWorkerClient = orchestrationWorkerClient;
        }

        public async Task<OrchestrationInstance> Run(TemplateOrchestrationInput args)
        {
            return await orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = args.DeploymentId,
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = TemplateOrchestrationUri
                },
                Input = DataConverter.Serialize(args)
            });
        }
    }
}