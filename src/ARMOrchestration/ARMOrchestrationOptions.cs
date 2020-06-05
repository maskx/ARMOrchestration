namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationOptions
    {
        /// <summary>
        /// Idel time when no dependsOn resource completed
        /// </summary>
        public int DependsOnIdelMilliseconds { get; set; } = 500;

        public DatabaseConfig Database { get; set; } = new DatabaseConfig();
    }
}