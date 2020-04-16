using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace ARMCreatorTest.TestARMFunctions
{
    internal class CustomARMFunctionTest
    {
        public void resourcegroup()
        {
            ARMFunctions functions = new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()
                {
                    //ListFunction = (sp, cxr, resourceId, apiVersion, functionValues, value) =>
                    //{
                    //    return new TaskResult() { Content = value };
                    //}
                }), null,
                new MockInfrastructure(null));
            functions.SetFunction("resourcegroup", (args, cxt) =>
            {
                if (!cxt.TryGetValue("armcontext", out object armcxt))
                    return;
                var input = armcxt as DeploymentOrchestrationInput;
                JObject obj = new JObject();
                obj.Add("id", $"/subscription/{input.SubscriptionId}/resourceGroups/{input.ResourceGroup}");
                obj.Add("name", input.ResourceGroup);
                obj.Add("type", "xxxx.Resources/resourceGroups");
                obj.Add("location", "");
                args.Result = new JsonValue(obj.ToString(Newtonsoft.Json.Formatting.None));
            });
        }
    }
}