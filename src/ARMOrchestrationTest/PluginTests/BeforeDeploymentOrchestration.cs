using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class BeforeDeploymentOrchestration : TaskOrchestration<TaskResult, DeploymentOrchestrationInput>
    {
        public override Task<TaskResult> RunTask(OrchestrationContext context, DeploymentOrchestrationInput input)
        {
            string s = input.Template.Outputs;
            if (string.IsNullOrEmpty(s))
                s = "{}";
            var j = JObject.Parse(s);
            var p = new JObject
            {
                { "type", "string" },
                { "value", "BeforeDeploymentOrchestration" }
            };
            j.Add("BeforeDeploy", p);
            // TODO: need find a new way to do this test
            //input.Template.Outputs = j.ToString(Newtonsoft.Json.Formatting.None);
            return Task.FromResult(new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) });
        }
    }
}