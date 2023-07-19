// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.IO;

namespace LicensePlateProcessingFunctions
{
	public static class ProcessImage
	{
		[FunctionName("ProcessImage")]
		public static void Run([EventGridTrigger] EventGridEvent eventGridEvent, [Blob(blobPath: "{data.url}", access: FileAccess.Read, Connection = "plateImagesStorageConnection")] Stream incomingPlateImageBlob, ILogger log)
		{
			log.LogInformation(eventGridEvent.Data.ToString());
		}
	}
}
