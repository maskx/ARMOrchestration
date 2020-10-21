using Newtonsoft.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#templatelink"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#TemplateLink"/>
    [JsonObject(MemberSerialization.OptOut)]
    public class TemplateLink
    {
        public string Id { get; set; }
        /// <summary>
        /// The URI of the template to deploy. Use either the uri or id property, but not both
        /// </summary>
        public string Uri { get; set; }
        /// <summary>
        /// Applicable only if this template link references a Template Spec. This relativePath property can optionally be used to reference a Template Spec artifact by path.
        /// </summary>
        public string RelativePath { get; set; }
        /// <summary>
        /// If included, must match the ContentVersion in the template.
        /// </summary>
        public string ContentVersion { get; set; }
    }
}