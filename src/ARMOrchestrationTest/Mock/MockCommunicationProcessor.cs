using ARMCreatorTest;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.Mock
{
    public class MockCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker CommunicationWorker { get; set; }

        public Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs)
        {
            List<CommunicationJob> rtv = new List<CommunicationJob>();
            foreach (var job in jobs)
            {
                job.ResponseCode = 200;
                job.ResponseContent = "MockCommunicationProcessor";
                if (job.RequestTo == RequestAction.ReadyResource.ToString())
                {
                    job.ResponseContent = TestHelper.GetJsonFileContent("mock/response/ReferenceExample");
                }
                job.Status = CommunicationJob.JobStatus.Completed;
                rtv.Add(job);
            }

            return Task.FromResult(rtv.ToArray());
        }
    }
}