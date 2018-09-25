using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace EindopdrachtCloudIvo
{
    public static class EindopdrachtQueue
    {
        const string StorageAccountName = "eindopdrachtopslag";
        const string StorageAccountKey = "rweS3a9pa1h7B6Ca4ag4wnb7Kai8pw005ORAyKq1OdVYfU2KLqSvk5GZeLz0adLObxfKiKSwPSPAYsA/ispGiA==";

        [FunctionName("EindopdrachtQueue")]
        public async static void RunAsync([QueueTrigger("eindopdrachtqueue", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            log.LogInformation("queue started");
            Coordinates coordinates = JsonConvert.DeserializeObject<Coordinates>(myQueueItem);
            string latitude = coordinates.latitude;
            string longitude = coordinates.longitude;
            string uniqueId = coordinates.uniqueID;
            HttpClient httpClient = new HttpClient();
            string openweatherAPIKey = "74c7658863f4ef5ca3ce2902d898a402";
            string url = "http://api.openweathermap.org/data/2.5/forecast?lat=" + latitude + "&lon=" + longitude + "&APPID=" + openweatherAPIKey;
            var responseString = await httpClient.GetStringAsync(url);
            RootObject weatherInfo = (RootObject) JsonConvert.DeserializeObject<RootObject>(responseString);
            double tempCelcius = weatherInfo.list.FirstOrDefault().main.temp - 272.15;
            string bierText = null;
            if(tempCelcius > 18)
            {
                bierText = "Koop bier!";
            } else
            {
                bierText = "Koop geen bier!";
            }

            url = "https://atlas.microsoft.com/map/static/png?subscription-key=EU0drBsqSKOc4L3-OAfKmiDBLiUbe2HnFsn1S1xrAmY&api-version=1.0&layer=basic&style=main&center=" + longitude + "," + latitude + "&language=nl-NL";
            HttpResponseMessage mapsResponse = await httpClient.GetAsync(url);
            var storageAccount = new CloudStorageAccount(new StorageCredentials(StorageAccountName, StorageAccountKey), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("mapblob");
            CloudBlockBlob blob = container.GetBlockBlobReference(uniqueId);

            if (mapsResponse.IsSuccessStatusCode)
            {
                System.IO.Stream stream = await mapsResponse.Content.ReadAsStreamAsync();
                stream = AddTextToImage(stream, (bierText, (10, 20)));
                await blob.UploadFromStreamAsync(stream);
                log.LogInformation("Image uploaded.");
            }
        }

        public static Stream AddTextToImage(Stream imageStream, params (string text, (float x, float y) position)[] texts)
        {
            var memoryStream = new MemoryStream();

            var image = Image.Load(imageStream);

            image
                .Clone(img =>
                {
                    foreach (var (text, (x, y)) in texts)
                    {
                        img.DrawText(text, SystemFonts.CreateFont("Verdana", 24), Rgba32.Chocolate, new PointF(x, y));
                    }
                })
                .SaveAsPng(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }

    }

}
