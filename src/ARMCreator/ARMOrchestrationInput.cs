using maskx.OrchestrationCreator.ARMTemplate;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestrationInput
    {
        public Template Template { get; set; }
        public string Parameters { get; set; }

        /// <summary>
        /// Complete  or Incremental
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string Name { get; set; }
    }
}