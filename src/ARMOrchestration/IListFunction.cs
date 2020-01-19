namespace maskx.ARMOrchestration
{
    public interface IListFunction
    {
        string Query(string resourceId, string apiVersion, string functionValues = "", string value = "");
    }
}