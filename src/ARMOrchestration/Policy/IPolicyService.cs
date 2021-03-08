using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;

namespace maskx.ARMOrchestration
{
    public interface IPolicyService
    {
        TaskResult EvaluateResource(Resource resource);
        TaskResult ValidateDeployment(Deployment deployment);
    }
}
