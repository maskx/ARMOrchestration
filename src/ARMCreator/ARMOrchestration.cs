using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using maskx.OrchestrationCreator.Extensions;
using Newtonsoft.Json.Linq;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestration : TaskOrchestration<string, ARMOrchestrationInput>
    {
        public override async Task<string> RunTask(OrchestrationContext context, ARMOrchestrationInput input)
        {
            List<Task> tasks = new List<Task>();
            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("parameters", input.Parameters);
            armContext.Add("parametersdefine", input.Template.Parameters);
            armContext.Add("variabledefine", input.Template.Variables);
            armContext.Add("userDefinedFunctions", input.Template.Functions);
            if (!string.IsNullOrEmpty(input.Template.Resources))
            {
                JsonDocument resDoc = JsonDocument.Parse(input.Template.Resources);
                var resources = resDoc.RootElement;
                for (int i = 0; i < resources.GetArrayLength(); i++)
                {
                    var resource = resources[i];
                    var p = new CreateOrUpdateInput()
                    {
                    };
                    tasks.Add(context.CreateSubOrchestrationInstance<string>(typeof(CreateOrUpdateOrchestration), p));
                }
            }
            Task.WaitAll(tasks.ToArray());
            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                return ARMFunctions.GetOutputs(input.Template.Outputs, armContext);
            }
            return string.Empty;
        }
    }
}