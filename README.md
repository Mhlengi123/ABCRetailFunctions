# ABC Retail Functions — CLDV7112 Project 2

An Azure Functions app (.NET 8, isolated worker) that expands Project 1
(`ABCRetailStorageApp`) with four HTTP-triggered functions, one per required
Azure Storage capability:

| Function | Method | Route | Purpose |
|---|---|---|---|
| `StoreTableInfo` | POST | `/api/table` | Stores a customer/product record in Azure Table Storage |
| `WriteBlobStorage` | POST | `/api/blob?fileName=...` | Uploads a file to Azure Blob Storage |
| `WriteQueueTransaction` | POST | `/api/queue` | Writes a transaction message to Azure Queue Storage |
| `ReadQueueTransactions` | GET | `/api/queue` | Reads (peeks) the current queue messages |
| `WriteAzureFile` | POST | `/api/file` | Writes a text file to Azure File Storage |

All four functions point at the **same storage account and resources** as
Project 1 (same table, container, queue and file share names), so anything
written through a function shows up in the same Azure Portal views you
already used for Project 1's screenshots.

## 1. Prerequisites

- Visual Studio 2022 with the **Azure development** workload (this adds the
  Functions project templates and local debugging support).
- .NET 8 SDK.
- The same Azure Storage Account you used for Project 1.

## 2. Configure the app

Open `local.settings.json` and replace the placeholder with your real
connection string (the same one from Project 1's `appsettings.json`):

```json
"AzureStorage:ConnectionString": "PASTE_YOUR_AZURE_STORAGE_CONNECTION_STRING_HERE"
```

`local.settings.json` is excluded from publish and should **not** be
committed with your real key inside it if your repo is public — use
**Manage User Secrets** in Visual Studio for local development instead, the
same way Project 1's README recommends.

## 3. Run locally

Press **F5** in Visual Studio, or:

```
dotnet restore
func start
```

The Functions runtime will print the local URLs for each endpoint, e.g.
`http://localhost:7071/api/table`. Test each one with Postman, curl, or your
browser (for the GET endpoint):

```
curl -X POST http://localhost:7071/api/table ^
  -H "Content-Type: application/json" ^
  -d "{\"partitionKey\":\"Product\",\"name\":\"Wireless Mouse\",\"price\":249.99}"

curl -X POST "http://localhost:7071/api/blob?fileName=test.jpg" ^
  -H "Content-Type: image/jpeg" --data-binary "@C:\path\to\test.jpg"

curl -X POST http://localhost:7071/api/queue ^
  -H "Content-Type: application/json" ^
  -d "{\"action\":\"Payment received\",\"details\":\"Order #1042 - R459.00\"}"

curl http://localhost:7071/api/queue

curl -X POST http://localhost:7071/api/file ^
  -H "Content-Type: application/json" ^
  -d "{\"fileName\":\"function-log.txt\",\"content\":\"Function ran successfully.\"}"
```

Confirm each write in the Azure Portal (Storage Browser) exactly as you did
for Project 1, and take your screenshots there plus in the terminal/Postman
response.

## 4. Push to GitHub

Add this as its own repository (or a sub-folder of your existing Project 1
repo, whichever your lecturer's format expects):

```
git init
git add .
git commit -m "Initial commit: ABC Retail Functions - Project 2"
git remote add origin <your-repo-url>
git push -u origin main
```

## 5. Deploy to an Azure Function App

In Visual Studio: right-click the project → **Publish** → **Azure** →
**Azure Function App (Windows or Linux)** → create/select a target Function
App (separate from the App Service used for the Project 1 web app).

After publishing, go to the Function App in the Azure Portal →
**Configuration → Application settings**, and add:

- Name: `AzureStorage__ConnectionString`
- Value: your real Storage Account connection string
- (repeat for `AzureStorage__TableName`, `AzureStorage__BlobContainerName`,
  `AzureStorage__QueueName`, `AzureStorage__FileShareName`,
  `AzureStorage__FileDirectoryName` if you changed any of the defaults)

Test the deployed endpoints the same way, replacing `localhost:7071` with
your Function App's URL, e.g. `https://<functionapp-name>.azurewebsites.net/api/table`.

## 6. Take your screenshots

For the submission document, capture for each of the four functions:
- The function's code (or a clear screenshot of it) in the Azure Portal /
  Visual Studio.
- The function running successfully (Postman/curl response, or the
  `func start` / Azure Portal log output).
- The resulting data in the Azure Portal Storage Browser (the new table
  row, the new blob, the message in the queue, the new file).

## Project structure

```
ABCRetailFunctions/
├── Functions/       # One function class per Azure Storage capability
├── Models/          # CustomerProductEntity, OrderMessage (same shape as Project 1)
├── Program.cs        # Isolated worker host
├── host.json
└── local.settings.json
```
