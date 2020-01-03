namespace maskx.ARMOrchestration.Activities
{
    public class DeploymentDetailActivityInput
    {
        public string DeploymentId { get; set; }
        public string InstanceId { get; set; }
        public string ExectutionId { get; set; }
        public string ResuorceId { get; set; }
        public string Resource { get; set; }
        public string Type { get; set; }
        public DeploymentStatus Status { get; set; }
        public string ParentId { get; set; }
        public string Result { get; set; }

        public enum DeploymentStatus
        {
        }
    }
}