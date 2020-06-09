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
                if (job.RuleField.ContainsKey("Type") && job.RuleField["Type"].ToString() == "Test.Mock/HasResourceFail"
                    && job.RuleField["Name"].ToString() == "fail")
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