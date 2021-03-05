namespace maskx.ARMOrchestration.Orchestrations
{
    public class WaitDependsOnOrchestrationInput : ResourceOrchestrationInput
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
    }
}