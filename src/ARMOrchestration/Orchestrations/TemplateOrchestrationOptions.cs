namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestrationOptions
    {
        public BuitinServiceTypes BuitinServiceTypes { get; set; }
        public string ConnectionString { get; set; }
    }

    public class BuitinServiceTypes
    {
        public string ResourceGroup { get; set; }
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
    }
}