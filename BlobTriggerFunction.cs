using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;

namespace AgriStreamFunctions
{
    public class BlobTriggerFunction
    {
        private readonly ILogger<BlobTriggerFunction> _logger;

        public BlobTriggerFunction(ILogger<BlobTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function("BlobTriggerFunction")]
        public async Task Run(
            [BlobTrigger("drone-images/{name}", Connection = "StorageConnection")]
            Stream stream,
            string name)
        {
            _logger.LogInformation($"📷 Drone image uploaded: {name}, Size: {stream.Length} bytes");

            // Write audit entry to Table Storage
            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("StorageConnection")!,
                "AuditLog");

            await tableClient.CreateIfNotExistsAsync();

            var audit = new TableEntity("DroneImages", Guid.NewGuid().ToString())
            {
                ["ImageName"] = name,
                ["SizeBytes"] = stream.Length,
                ["UploadedAt"] = DateTime.UtcNow.ToString()
            };

            await tableClient.AddEntityAsync(audit);
            _logger.LogInformation("✅ Audit entry written to Table Storage");
        }
    }
}