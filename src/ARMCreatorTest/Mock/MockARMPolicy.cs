using maskx.OrchestrationCreator;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace ARMCreatorTest.Mock
{
    public class MockARMPolicy : IARMPolicy
    {
        public async Task<TaskResult> Apply(string str)
        {
            return new TaskResult() { Code = 200 };
        }

        public async Task<TaskResult> Check(string str)
        {
            return new TaskResult() { Code = 200 };
        }
    }
}