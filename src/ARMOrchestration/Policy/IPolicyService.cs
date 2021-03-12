using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration
{
    public interface IPolicyService
    {
        TaskResult EvaluateResource(Resource resource);
        TaskResult EvaluateDeployment(Deployment deployment);
    }
}
