using maskx.OrchestrationService.Activity;
using System;

namespace maskx.OrchestrationCreator.Orchestrations
{
    public class ResourceOrchestrationOptions
    {
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckPolicyRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetBeginCreateResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCheckQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCreateResourceRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitQoutaRequestInput { get; set; }
        public Func<ResourceOrchestrationInput, AsyncRequestInput> GetCommitResourceRequestInput { get; set; }
        public Func<string, string, AsyncRequestInput> GetLockCheckRequestInput { get; set; }
    }
}