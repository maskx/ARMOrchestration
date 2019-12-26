using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;

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