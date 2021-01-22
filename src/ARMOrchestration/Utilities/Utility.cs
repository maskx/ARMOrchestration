using Microsoft.SqlServer.Management.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Utilities
{
    public static class Utility
    {
        public static async Task ExecuteSqlScriptAsync(string scriptContent, string connectionString)
        {
            await using Microsoft.Data.SqlClient.SqlConnection scriptRunnerConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            var serverConnection = new ServerConnection(scriptRunnerConnection);
            serverConnection.ExecuteNonQuery(scriptContent);
        }
        internal static async Task ExecuteSqlScriptAsync(string scriptName, DatabaseConfig options)
        {
            string scriptContent = string.Format(await GetScriptTextAsync(scriptName), options.SchemaName, options.HubName);
            await ExecuteSqlScriptAsync(scriptContent, options.ConnectionString);
        }
        internal static async Task<string> GetScriptTextAsync(string scriptName, string schemaName, string hubName)
        {
            return string.Format(await GetScriptTextAsync(scriptName), schemaName, hubName);
        }
        public static async Task<string> GetScriptTextAsync(string scriptName)
        {
            var assembly = typeof(Utility).Assembly;
            string assemblyName = assembly.GetName().Name;
            if (!scriptName.StartsWith(assemblyName))
                scriptName = $"{assembly.GetName().Name}.Scripts.{scriptName}";

            using Stream resourceStream = assembly.GetManifestResourceStream(scriptName);
            if (resourceStream == null)
                throw new ArgumentException($"Could not find assembly resource named '{scriptName}'.");
            using var reader = new StreamReader(resourceStream);
            return await reader.ReadToEndAsync();
        }
    }
}
