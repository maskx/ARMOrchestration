using DurableTask.Core;
using maskx.ARMOrchestration;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class AfterDeploymentOrhcestration : TaskOrchestration<TaskResult, Deployment>
    {
        private readonly IServiceProvider _ServiceProvider;

        public AfterDeploymentOrhcestration(IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
        }

        public override Task<TaskResult> RunTask(OrchestrationContext context, Deployment input)
        {
            input.ServiceProvider = _ServiceProvider;
            string s = input.Template.Outputs;
            if (string.IsNullOrEmpty(s))
                s = "{}";
            var j = JObject.Parse(s);
            var p = new JObject
            {
                { "type", "string" },
                { "value", "AfterDeploymentOrhcestration" }
            };
            j.Add("AfterDeploy", p);
            input.Template.Outputs = j.ToString(Newtonsoft.Json.Formatting.None);
            return Task.FromResult(new TaskResult() { Code = 200, Content = input });
        }
    }
}