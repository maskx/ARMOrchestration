using maskx.OrchestrationCreator;
using maskx.OrchestrationCreator.Orchestrations;
using Newtonsoft.Json.Linq;

namespace ARMCreatorTest.TestARMFunctions
{
    internal class CustomARMFunctionTest
    {
        public void resourcegroup()
        {
            ARMFunctions.SetFunction("resourcegroup", (args, cxt) =>
            {
                if (!cxt.TryGetValue("armcontext", out object armcxt))
                    return;
                var input = armcxt as ARMOrchestrationInput;
                JObject obj = new JObject();
                obj.Add("id", $"/subscriptions/{input.SubscriptionId}/resourceGroups/{input.ResourceGroup}");
                obj.Add("name", input.ResourceGroup);
                obj.Add("type", "xxxx.Resources/resourceGroups");
                obj.Add("location", "");
                args.Result = new JsonValue(obj.ToString(Newtonsoft.Json.Formatting.None));
            });
        }
    }
}