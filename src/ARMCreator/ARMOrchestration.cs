using DurableTask.Core;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestration : TaskOrchestration<TaskResult, string>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            ARMOrchestrationInput input = this.DataConverter.Deserialize<ARMOrchestrationInput>(arg);
            List<Task> tasks = new List<Task>();
            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("parameters", input.Parameters);
            armContext.Add("parametersdefine", input.Template.Parameters);
            armContext.Add("variabledefine", input.Template.Variables);
            armContext.Add("userDefinedFunctions", input.Template.Functions);

            foreach (var resource in input.Template.Resources)
            {
                if (null == resource.Copy)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = resource,
                        OrchestrationContext = armContext
                    };
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                }
                else
                {
                    var copy = resource.Copy;
                    var copyindex = new Dictionary<string, int>()
                    {
                        { copy.Name,0 }
                    };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>();
                    copyContext.Add("parameters", input.Parameters);
                    copyContext.Add("parametersdefine", input.Template.Parameters);
                    copyContext.Add("variabledefine", input.Template.Variables);
                    copyContext.Add("userDefinedFunctions", input.Template.Functions);
                    copyContext.Add("copyindex", copyindex);
                    copyContext.Add("copyindexcurrentloopname", copy.Name);
                    for (int i = 0; i < copy.Count; i++)
                    {
                        copyindex[copy.Name] = i;
                        var par = new ResourceOrchestrationInput()
                        {
                            Resource = resource,
                            OrchestrationContext = copyContext
                        };
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                    }
                }
            }
            await Task.WhenAll(tasks.ToArray());
            string rtv = string.Empty;
            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                rtv = ARMFunctions.GetOutputs(input.Template.Outputs, armContext);
            }
            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}