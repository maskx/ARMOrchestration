using maskx.OrchestrationCreator;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ARMCreatorTest.Mock
{
    public class MockQuota : IQuota
    {
        public async Task<TaskResult> Begin()
        {
            return new TaskResult() { Code = 200 };
        }

        public async Task<TaskResult> Commit()
        {
            return new TaskResult() { Code = 200 };
        }

        public async Task<TaskResult> Rollback()
        {
            return new TaskResult() { Code = 200 };
        }
    }
}