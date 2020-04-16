using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class AfterDeploymentOrhcestration : TaskOrchestration<TaskResult, DeploymentOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, DeploymentOrchestrationInput input)
        {
            string s = input.Template.Outputs;
            if (string.IsNullOrEmpty(s))
                s = "{}";
            var j = JObject.Parse(s);
            var p = new JObject();
            p.Add("type", "string");
            p.Add("value", "AfterDeploymentOrhcestration");
            j.Add("AfterDeploy", p);
            input.Template.Outputs = j.ToString(Newtonsoft.Json.Formatting.None);
            return new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) };
        }
    }
}