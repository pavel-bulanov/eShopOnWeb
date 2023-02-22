using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace OrderItemsReceiver
{
    public class Function1
    {
        [FunctionName("Order")]
        public static async Task Run(

            [ServiceBusTrigger("orders", Connection = "OrdersBus")]
            string requestBody,
            [Blob("orders/{DateTime.Now.ToString()}.json", FileAccess.Write, Connection = "OrdersStore")] Stream blob,
            ILogger log)
        {
            log.LogInformation("C# Service bus trigger has started.");

            using var sw = new StreamWriter(blob);
            await sw.WriteAsync(requestBody);
        }
    }
}
