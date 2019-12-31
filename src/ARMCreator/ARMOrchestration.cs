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
            armContext.Add("armcontext", input);
            var template = new ARMTemplate.Template(input.Template);
            foreach (var resource in template.Resources)
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
                    var loopName = ARMFunctions.Evaluate(copy.Name, armContext).ToString();
                    var loopCount = (int)ARMFunctions.Evaluate(copy.Count, armContext);
                    var copyindex = new Dictionary<string, int>()
                    {
                        { loopName,0 }
                    };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>();
                    copyContext.Add("armcontext", input);
                    copyContext.Add("copyindex", copyindex);
                    copyContext.Add("copyindexcurrentloopname", loopName);
                    for (int i = 0; i < loopCount; i++)
                    {
                        copyindex[loopName] = i;
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
            if (!string.IsNullOrEmpty(template.Outputs))
            {
                rtv = ARMFunctions.GetOutputs(template.Outputs, armContext);
            }
            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}