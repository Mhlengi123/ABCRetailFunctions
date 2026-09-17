using System.Net;
using System.Text.Json;
using ABCRetailFunctions.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailFunctions.Functions
{
    // Function 3: a queue that is written to AND read from for transaction
    // information. WriteQueueTransaction (POST) adds a new transaction message;
    // ReadQueueTransactions (GET) peeks the queue's current contents without
    // removing anything, so it stays visible for a screenshot.
    public class QueueFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public QueueFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<QueueFunction>();
            _configuration = configuration;
        }

        private QueueClient GetQueueClient()
        {
            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var queueName = _configuration["AzureStorage:QueueName"] ?? "order-processing-queue";
            var client = new QueueClient(connectionString, queueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            client.CreateIfNotExists();
            return client;
        }

        // POST /api/queue
        // Body: { "action": "Payment received", "details": "Order #1042 - R459.00" }
        [Function("WriteQueueTransaction")]
        public async Task<HttpResponseData> Write(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "queue")] HttpRequestData req)
        {
            _logger.LogInformation("WriteQueueTransaction function triggered.");

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            OrderMessage input;
            try
            {
                input = JsonSerializer.Deserialize<OrderMessage>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new OrderMessage();
            }
            catch (JsonException)
            {
                var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalid.WriteStringAsync("Request body must be valid JSON.");
                return invalid;
            }

            var queueClient = GetQueueClient();
            var json = JsonSerializer.Serialize(input);
            await queueClient.SendMessageAsync(json);
            _logger.LogInformation("Sent transaction message {OrderId} to the queue.", input.OrderId);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(input);
            return response;
        }

        // GET /api/queue
        [Function("ReadQueueTransactions")]
        public async Task<HttpResponseData> Read(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queue")] HttpRequestData req)
        {
            _logger.LogInformation("ReadQueueTransactions function triggered.");

            var queueClient = GetQueueClient();
            var results = new List<OrderMessage>();
            PeekedMessage[] peeked = await queueClient.PeekMessagesAsync(maxMessages: 10);

            foreach (var msg in peeked)
            {
                try
                {
                    var order = JsonSerializer.Deserialize<OrderMessage>(msg.MessageText);
                    if (order != null) results.Add(order);
                }
                catch (JsonException)
                {
                    // Skip any malformed message rather than failing the whole read.
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(results);
            return response;
        }
    }
}
