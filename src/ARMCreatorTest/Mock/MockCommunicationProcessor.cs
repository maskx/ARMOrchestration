﻿using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARMCreatorTest.Mock
{
    public class MockCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;

        public async Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs)
        {
            List<CommunicationJob> rtv = new List<CommunicationJob>();
            foreach (var job in jobs)
            {
                job.ResponseCode = 200;
                job.ResponseContent = "MockCommunicationProcessor";
                rtv.Add(job);
            }

            return rtv.ToArray();
        }
    }
}