using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public interface IARMPolicy
    {
        Task<TaskResult> Check(string str);

        Task<TaskResult> Apply(string str);
    }
}