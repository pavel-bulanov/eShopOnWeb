using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DeliveryOrderProcessor
{
    public static class Function1
    {
        [FunctionName("Order")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "ordersdb",
                containerName: "orders",
                Connection = "CosmosDBConnection")] IAsyncCollector<dynamic> documents,
                ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function started");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var document = new { Payload = data, id = Guid.NewGuid() };
            await documents.AddAsync(document);
        }
}
}
