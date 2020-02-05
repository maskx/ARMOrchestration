using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARMOrchestrationTest.Mock
{
    public class MockInfrastructure : IInfrastructure
    {
        public AsyncRequestInput GetRequestInput(RequestOrchestrationInput input)
        {
            var r = TestHelper.CreateAsyncRequestInput("MockCommunicationProcessor", input.Resource);
            r.RequestTo = input.RequestAction.ToString();
            return r;
        }

        public TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "")
        {
            return new TaskResult() { Content = value };
        }

        public TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false)
        {
            var pars = resourceName.Split('/');
            if (pars.Length >= 5)
            {
                if ("resourceGroups".Equals(pars[3], StringComparison.OrdinalIgnoreCase))
                {
                    return new TaskResult()
                    {
                        Code = 200,
                        Content = TestHelper.GetJsonFileContent("Mock/Response/getresourcegroup")
                    };
                }
            }
            return new TaskResult() { Code = 200 };
        }

        public BuiltinServiceTypes BuitinServiceTypes { get; set; } = new BuiltinServiceTypes();
        public List<string> ExtensionResources { get; set; } = new List<string>() { "tags" };
    }
}