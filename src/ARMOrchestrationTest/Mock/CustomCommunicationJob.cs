using maskx.OrchestrationService.Worker;

namespace ARMOrchestrationTest.Mock
{
    public class CustomCommunicationJob : CommunicationJob
    {
        public string ApiVersion { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string SKU { get; set; }
        public string Kind { get; set; }
        public string Plan { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ResourceGroup { get; set; }
    }
}
