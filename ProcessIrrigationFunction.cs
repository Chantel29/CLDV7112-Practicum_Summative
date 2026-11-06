using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace AgriStreamFunctions
{
    public class ProcessIrrigationFunction
    {
        private readonly ILogger<ProcessIrrigationFunction> _logger;

        public ProcessIrrigationFunction(ILogger<ProcessIrrigationFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessIrrigation")]
        public async Task Run(
            [QueueTrigger("irrigation-commands", Connection = "StorageConnection")]
            string message)
        {
            _logger.LogInformation($"🔄 Processing irrigation command: {message}");

            string? sqlConn = Environment.GetEnvironmentVariable("SqlConnectionString");

            using var connection = new SqlConnection(sqlConn);
            await connection.OpenAsync();

            // First create Devices table if it doesn't exist
            string createTable = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Devices')
                CREATE TABLE Devices (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    DeviceType NVARCHAR(50),
                    Status NVARCHAR(50),
                    LastUpdated DATETIME
                )";

            using (var cmd = new SqlCommand(createTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert a device record if none exists
            string insertDevice = @"IF NOT EXISTS (SELECT * FROM Devices WHERE DeviceType = 'Irrigation')
                INSERT INTO Devices (DeviceType, Status, LastUpdated) VALUES ('Irrigation', 'Idle', GETUTDATE())";

            using (var cmd = new SqlCommand(insertDevice, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Update device status
            string updateQuery = @"UPDATE Devices SET Status = 'Irrigating', LastUpdated = @time WHERE DeviceType = 'Irrigation'";

            using (var cmd = new SqlCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@time", DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("✅ Device status updated to 'Irrigating' in SQL Database");
        }
    }
}