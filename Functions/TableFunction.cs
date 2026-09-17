using System.Net;
using System.Text.Json;
using ABCRetailFunctions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailFunctions.Functions
{
    // Function 1: stores customer/product information in Azure Table Storage.
    // Reuses the same CustomerProductTable that Project 1's web app writes to,
    // so records added here appear alongside the ones added through the site.
    public class TableFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TableFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<TableFunction>();
            _configuration = configuration;
        }

        // POST /api/table
        // Body: { "partitionKey": "Product", "name": "Wireless Mouse", "description": "...", "price": 249.99 }
        [Function("StoreTableInfo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table")] HttpRequestData req)
        {
            _logger.LogInformation("StoreTableInfo function triggered.");

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            CustomerProductEntity? input;
            try
            {
                input = JsonSerializer.Deserialize<CustomerProductEntity>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalid.WriteStringAsync("Request body must be valid JSON.");
                return invalid;
            }

            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body must include at least a 'name'.");
                return badResponse;
            }

            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var tableName = _configuration["AzureStorage:TableName"] ?? "CustomerProductTable";

            var serviceClient = new TableServiceClient(connectionString);
            await serviceClient.CreateTableIfNotExistsAsync(tableName);
            var tableClient = serviceClient.GetTableClient(tableName);

            await tableClient.AddEntityAsync(input);
            _logger.LogInformation("Stored entity {RowKey} in {TableName}.", input.RowKey, tableName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(input);
            return response;
        }
    }
}
