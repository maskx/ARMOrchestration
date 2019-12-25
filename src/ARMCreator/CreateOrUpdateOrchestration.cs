using DurableTask.Core;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class CreateOrUpdateOrchestration : TaskOrchestration<string, CreateOrUpdateInput>
    {
        public override async Task<string> RunTask(OrchestrationContext context, CreateOrUpdateInput input)
        {
            if (!string.IsNullOrEmpty(input.Resource.Condition))
            {
                var c = ARMFunctions.Evaluate(input.Resource.Condition, input.OrchestrationContext);
                if ((bool)c)
                    return string.Empty;
            }
            if (input.Resource.DependsOn.Count > 0)
            {
            }
            return string.Empty;
        }
    }
}