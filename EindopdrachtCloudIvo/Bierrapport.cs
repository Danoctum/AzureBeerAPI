using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace EindopdrachtCloudIvo
{
    public class Coordinates
    {
        public string longitude { get; set; }
        public string latitude { get; set; }
        public string uniqueID { get; set; }

        public Coordinates(string longitude, string latitude, string uniqueID)
        {
            this.longitude = longitude;
            this.latitude = latitude;
            this.uniqueID = uniqueID;
        }
    }

    public static class Bierrapport
    {
        const string StorageAccountName = "eindopdrachtopslag";
        const string StorageAccountKey = "rweS3a9pa1h7B6Ca4ag4wnb7Kai8pw005ORAyKq1OdVYfU2KLqSvk5GZeLz0adLObxfKiKSwPSPAYsA/ispGiA==";

        [FunctionName("Bierrapport")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req, ILogger log)
        {
            log.LogInformation("Bierrapport aangevraagd.");

            // parse query parameter
            var hoogte = req.Query["long"].ToString();
            var breedte = req.Query["lat"].ToString();
            log.LogInformation(hoogte);

            if (hoogte != null && breedte != null)
            {
                // Handle requests
                var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), true);
                var client = storageAccount.CreateCloudQueueClient();
                var queue = client.GetQueueReference("eindopdrachtqueue");
                await queue.CreateIfNotExistsAsync();
                String uniqueId = Guid.NewGuid().ToString() + ".png";
                Coordinates coordinates = new Coordinates(hoogte, breedte, uniqueId);
                string json = JsonConvert.SerializeObject(coordinates);
                await queue.AddMessageAsync(new CloudQueueMessage(json));

                return new OkObjectResult("https://eindopdrachtopslag.blob.core.windows.net/mapblob/" + uniqueId);
            } else
            {
                return new BadRequestResult();
            }
        }
    }
}
