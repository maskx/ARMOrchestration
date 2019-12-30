using DurableTask.Core;
using System;

namespace maskx.OrchestrationCreator.Activity
{
    public class SetDependsOnActivity : TaskActivity<string, string>
    {
        protected override string Execute(TaskContext context, string input)
        {
            throw new NotImplementedException();
        }
    }
}