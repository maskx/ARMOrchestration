using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARMCreatorTest.Mock
{
    public class MockCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;

        public Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs)
        {
            List<CommunicationJob> rtv = new List<CommunicationJob>();
            foreach (var job in jobs)
            {
                job.ResponseCode = job.RequestTo == RequestAction.CheckLock.ToString() ? 404 : 200;
                job.ResponseContent = "MockCommunicationProcessor";
                job.Status = CommunicationJob.JobStatus.Completed;
                rtv.Add(job);
            }

            return Task.FromResult(rtv.ToArray());
        }
    }
}