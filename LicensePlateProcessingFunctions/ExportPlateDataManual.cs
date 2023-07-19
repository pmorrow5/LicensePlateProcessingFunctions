using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using LicensePlateProcessingFunctions.CosmosLogic;
using LicensePlateProcessingFunctions.StorageAndFileProcessing;
using System.Linq;

namespace LicensePlateProcessingFunctions
{
    public static class ExportPlateDataManual
    {
        private static string ReadyForImportFileName = "YYYYMMDDHHMMSS_####_PlatesReadyForImport.csv";
        private static string ReadyForReviewFileName = "YYYYMMDDHHMMSS_####_PlatesProcessedButUnconfirmed.csv";

        [FunctionName("ExportPlateDataManual")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# export manual trigger function executed at: {DateTime.Now}");
            log.LogInformation("Finding license plate data to export");

            var cosmosEndpointURL = Environment.GetEnvironmentVariable("cosmosDBEndpointUrl");
            var cosmosDbAuthorizationKey = Environment.GetEnvironmentVariable("cosmosDBAuthorizationKey");
            var cosmosDatabaseId = Environment.GetEnvironmentVariable("cosmosDBDatabaseId");
            var cosmosContainerId = Environment.GetEnvironmentVariable("cosmosDBContainerId");

            var container = Environment.GetEnvironmentVariable("datalakeexportscontainer");
            var storageConnection = Environment.GetEnvironmentVariable("datalakeexportsconnection");

            int exportedConfirmedCount = 0;
            int exportedNonConfirmedCount = 0;
            var timeStamp = DateTime.Now.ToString("yyyyMMddhhmmss");

            //create the cosmos helper:
            var cosmosOperations = new CosmosOperations(cosmosEndpointURL, cosmosDbAuthorizationKey, cosmosDatabaseId, cosmosContainerId, log);

            //get plates to export
            var licensePlates = await cosmosOperations.GetLicensePlatesToExport();

            if (licensePlates != null && licensePlates.Any())
            {
                log.LogInformation($"Retrieved {licensePlates.Count} license plates");
                //get confirmed and unconfirmed plates:
                var confirmedPlates = licensePlates.Where(x => x.confirmed);
                var nonConfirmedPlates = licensePlates.Where(x => !x.confirmed);
                exportedConfirmedCount = confirmedPlates.Count();
                exportedNonConfirmedCount = nonConfirmedPlates.Count();

                //create a csv helper
                var createImportsCSVInMemory = new CreateCSVFromPlateDataInMemory();

                //create a storage helper
                var sh = new BlobStorageHelper(storageConnection, container, log);

                var success = false;
                if (confirmedPlates != null && confirmedPlates.Any())
                {
                    //get confirmed plates file name:
                    var fileNameReadyForImport = ReadyForImportFileName.Replace("YYYYMMDDHHMMSS", timeStamp).Replace("####", exportedConfirmedCount.ToString().PadLeft(4, '0'));

                    //get imports (confirmed plates) into memory stream csv
                    var readyForImportCSVStream = await createImportsCSVInMemory.CreateCSVStreamFromPlateData(confirmedPlates);

                    //upload to storage
                    success = await sh.UploadBlob(readyForImportCSVStream, fileNameReadyForImport);
                    if (success)
                    {
                        //mark as exported
                        await cosmosOperations.MarkLicensePlatesAsExported(confirmedPlates, true);
                        log.LogInformation($"Confirmed plates marked as exported and exported to file {fileNameReadyForImport}");
                        //report counts
                        log.LogInformation($"Exported {exportedConfirmedCount} confirmed license plates");
                    }
                }

                if (nonConfirmedPlates != null && nonConfirmedPlates.Any())
                {
                    //get reviewable plates into memory stream csv
                    var fileNameReadyForReview = ReadyForReviewFileName.Replace("YYYYMMDDHHMMSS", timeStamp).Replace("####", exportedNonConfirmedCount.ToString().PadLeft(4, '0'));
                    var readyForReviewCSVStream = await createImportsCSVInMemory.CreateCSVStreamFromPlateData(nonConfirmedPlates);

                    //upload to storage
                    success = await sh.UploadBlob(readyForReviewCSVStream, fileNameReadyForReview);
                    if (success)
                    {
                        //mark as exported
                        await cosmosOperations.MarkLicensePlatesAsExported(nonConfirmedPlates, false);
                        log.LogInformation($"Reviewable plates marked as exported and exported to file {fileNameReadyForReview}");
                        log.LogInformation($"Exported {exportedNonConfirmedCount} license plates for review");
                    }
                }
            }
            else
            {
                log.LogWarning("No license plates to export");
            }

            log.LogInformation("Completed processing of plates");

            string responseMessage = "Exported " + exportedConfirmedCount + " confirmed plates and " + exportedNonConfirmedCount + " reviewable plates";

            return new OkObjectResult(responseMessage);
        }
    }
}
