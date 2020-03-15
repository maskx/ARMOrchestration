namespace maskx.ARMOrchestration.Functions
{
    public static class ContextKeys
    {
        public const string ARM_CONTEXT = "armcontext";

        /// <summary>
        /// the value is List<string>
        /// </summary>
        public const string DEPENDSON = "dependson";

        public const string COPY_INDEX = "copyindex";
        public const string CURRENT_LOOP_NAME = "currentloopname";

        /// <summary>
        /// if this key is exist, then process for prepare, not care the value is true or false
        /// </summary>
        public const string IS_PREPARE = "isprepare";
    }
}