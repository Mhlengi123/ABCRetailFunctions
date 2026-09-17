using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailFunctions.Functions
{
    // Function 4: writes log/text content to Azure File Storage (Azure Files),
    // using the same logshare/logs directory as Project 1's web app.
    public class FileFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public FileFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<FileFunction>();
            _configuration = configuration;
        }

        public class FileRequest
        {
            public string FileName { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        // POST /api/file
        // Body: { "fileName": "function-log-2026-09-15.txt", "content": "Function ran successfully." }
        [Function("WriteAzureFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "file")] HttpRequestData req)
        {
            _logger.LogInformation("WriteAzureFile function triggered.");

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            FileRequest? input;
            try
            {
                input = JsonSerializer.Deserialize<FileRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                var invalid = req.CreateResponse(HttpStatusCode.BadRequest);
                await invalid.WriteStringAsync("Request body must be valid JSON.");
                return invalid;
            }

            if (input == null || string.IsNullOrWhiteSpace(input.FileName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body must include 'fileName' and 'content'.");
                return badResponse;
            }

            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var shareName = _configuration["AzureStorage:FileShareName"] ?? "logshare";
            var directoryName = _configuration["AzureStorage:FileDirectoryName"] ?? "logs";

            var shareClient = new ShareClient(connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(input.FileName);
            var bytes = Encoding.UTF8.GetBytes(input.Content);

            using var stream = new MemoryStream(bytes);
            await fileClient.CreateAsync(bytes.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), stream);
            _logger.LogInformation("Wrote {FileName} ({Size} bytes) to Azure Files.", input.FileName, bytes.Length);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { input.FileName, sizeBytes = bytes.Length });
            return response;
        }
    }
}
