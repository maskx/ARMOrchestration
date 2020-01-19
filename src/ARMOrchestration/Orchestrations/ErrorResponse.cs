namespace maskx.ARMOrchestration.Orchestrations
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#errorresponse"/>
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Target { get; set; }
    }
}