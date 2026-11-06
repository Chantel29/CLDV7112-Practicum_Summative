using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;

namespace AgriStreamFunctions
{
    public class TriggerIrrigationFunction
    {
        private readonly ILogger<TriggerIrrigationFunction> _logger;

        public TriggerIrrigationFunction(ILogger<TriggerIrrigationFunction> logger)
        {
            _logger = logger;
        }

        [Function("TriggerIrrigation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "irrigate")]
            HttpRequestData req)
        {
            _logger.LogInformation("💧 Irrigation command received");

            var queueClient = new QueueClient(
                Environment.GetEnvironmentVariable("StorageConnection")!,
                "irrigation-commands");

            await queueClient.CreateIfNotExistsAsync();

            string message = $"Irrigation command at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            await queueClient.SendMessageAsync(message);

            _logger.LogInformation("✅ Irrigation command added to queue");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Irrigation command queued successfully!");
            return response;
        }
    }
}