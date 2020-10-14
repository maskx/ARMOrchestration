namespace maskx.ARMOrchestration
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#erroradditionalinfo
    /// </summary>
    public class ErrorAdditionalInfo
    {
        public string Type { get; set; }
        public object Info { get; set; }
    }
}
