using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public interface IResource
    {
        Task<TaskResult> Begin();

        Task<TaskResult> Commit();

        Task<TaskResult> Rollback();
    }
}