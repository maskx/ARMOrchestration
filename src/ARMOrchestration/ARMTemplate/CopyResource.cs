using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class CopyResource : Resource
    {
        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize { get; set; }
    }
}
