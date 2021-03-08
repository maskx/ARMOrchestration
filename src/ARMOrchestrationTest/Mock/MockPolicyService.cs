using maskx.ARMOrchestration;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;

namespace ARMOrchestrationTest.Mock
{
    public class MockPolicyService : IPolicyService
    {
        public TaskResult EvaluateResource(Resource resource)
        {
            if (resource.Name.EndsWith("1Policy"))
            {
                resource.RawProperties = "{comment:'policy modified'}";
                return new TaskResult(200, "");
            }
            return new TaskResult(200, "");
        }

        public TaskResult ValidateDeployment(Deployment deployment)
        {
            return new TaskResult(200, "");
        }
    }
}
