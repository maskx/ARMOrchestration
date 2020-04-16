namespace maskx.ARMOrchestration
{
    public class BuiltinServiceTypes
    {
        public string ResourceGroup { get; set; } = "Microsoft.Resources/resourceGroups";
        public string Subscription { get; set; }
        public string ManagementGroup { get; set; }
        public string Deployments { get; set; } = "Microsoft.Resources/deployments";
        public string Copy { get; set; } = "copy";
    }
}