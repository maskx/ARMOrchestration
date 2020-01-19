namespace maskx.ARMOrchestration
{
    public interface IListFunction
    {
        string Query(string resourceName, string apiVersion, string functionValues);
    }
}