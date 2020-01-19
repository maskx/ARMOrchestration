namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#propertychangetype"/>
    public class PropertyChangeType
    {
        public string Array { get; set; }
        public string Create { get; set; }
        public string Delete { get; set; }
        public string Modify { get; set; }
    }
}