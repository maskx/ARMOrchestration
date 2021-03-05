using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration
{
    public interface IPolicyService
    {
        (bool Result,string Message) Evaluateesource(Resource resource);
        (bool Result, string Message) ValidateDeployment(Deployment deployment);
    }
}
