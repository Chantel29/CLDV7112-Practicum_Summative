using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Text.Json;

namespace AgriStreamFunctions
{
    public class GetTelemetryFunction
    {
        private readonly ILogger<GetTelemetryFunction> _logger;

        public GetTelemetryFunction(ILogger<GetTelemetryFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetTelemetry")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "telemetry")]
            HttpRequestData req)
        {
            _logger.LogInformation("🌾 Farmer requested telemetry data");

            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("StorageConnection")!,
                "TelemetryData");

            await tableClient.CreateIfNotExistsAsync();

            // Add sample telemetry data if table is empty
            var sample = new TableEntity("Sensor1", Guid.NewGuid().ToString())
            {
                ["SoilMoisture"] = 45.5,
                ["Temperature"] = 22.3,
                ["Timestamp"] = DateTime.UtcNow.ToString()
            };
            await tableClient.AddEntityAsync(sample);

            // Query all telemetry
            var results = new List<TableEntity>();
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                results.Add(entity);
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(results));
            return response;
        }
    }
}