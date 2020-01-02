namespace maskx.OrchestrationCreator.Orchestrations
{
    public class ARMOrchestrationOptions
    {
        public BuitinServiceTypes BuitinServiceTypes { get; set; }
    }

    public class BuitinServiceTypes
    {
        public string ResourceGroup { get; set; }
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
    }
}