using System.Net;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailFunctions.Functions
{
    // Function 2: writes uploaded product images/multimedia to Azure Blob Storage.
    // Send the file bytes as the raw request body and pass the desired file
    // name as a query string parameter, e.g. POST /api/blob?fileName=product1.jpg
    // (this keeps the function simple - a small change to the web app's own
    // upload form could also call this endpoint instead of BlobStorageService).
    public class BlobFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public BlobFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<BlobFunction>();
            _configuration = configuration;
        }

        [Function("WriteBlobStorage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blob")] HttpRequestData req)
        {
            _logger.LogInformation("WriteBlobStorage function triggered.");

            var query = HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Pass the file name in the 'fileName' query string parameter.");
                return badResponse;
            }

            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var containerName = _configuration["AzureStorage:BlobContainerName"] ?? "product-media";

            var containerClient = new BlobContainerClient(connectionString, containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var contentType = req.Headers.TryGetValues("Content-Type", out var values)
                ? values.First()
                : "application/octet-stream";

            await blobClient.UploadAsync(req.Body, new BlobHttpHeaders { ContentType = contentType });
            _logger.LogInformation("Uploaded blob {BlobName} to {ContainerName}.", blobName, containerName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var url = blobClient.CanGenerateSasUri
                ? blobClient.GenerateSasUri(sasBuilder).ToString()
                : blobClient.Uri.ToString();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { blobName, url });
            return response;
        }
    }
}
