using DurableTask.Core;
using maskx.ARMOrchestration;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class BeforeDeploymentOrchestration : TaskOrchestration<TaskResult, Deployment>
    {
        private readonly IServiceProvider _ServiceProvider;

        public BeforeDeploymentOrchestration(IServiceProvider serviceProvider)
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
                { "value", "BeforeDeploymentOrchestration" }
            };
            j.Add("BeforeDeploy", p);
            input.Template.Outputs = j.ToString(Newtonsoft.Json.Formatting.None);
            return Task.FromResult(new TaskResult() { Code = 200, Content =input });
        }
    }
}