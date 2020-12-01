using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.Mock
{
    public class MockCommunicationProcessor : ICommunicationProcessor<CustomCommunicationJob>
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;

        public Task<CustomCommunicationJob[]> ProcessAsync(params CustomCommunicationJob[] jobs)
        {
            List<CustomCommunicationJob> rtv = new List<CustomCommunicationJob>();
            foreach (var job in jobs)
            {
                if (job.Type == "Test.Mock/HasResourceFail" && job.Name == "fail")
                {
                    job.ResponseCode = 500;
                    job.ResponseContent = "MockCommunicationProcessor";
                    job.Status = CommunicationJob.JobStatus.Completed;
                }
                else
                {
                    job.ResponseCode = 200;
                    job.ResponseContent = "MockCommunicationProcessor";
                    job.Status = CommunicationJob.JobStatus.Completed;
                }

                rtv.Add(job);
            }

            return Task.FromResult(rtv.ToArray());
        }
    }
}