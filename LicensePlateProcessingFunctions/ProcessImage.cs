// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.IO;
using System.Threading.Tasks;

namespace LicensePlateProcessingFunctions
{
	public static class ProcessImage
	{
		[FunctionName("ProcessImage")]
		public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, [Blob(blobPath: "{data.url}", access: FileAccess.Read, Connection = "plateImagesStorageConnection")] Stream incomingPlateImageBlob, ILogger log)
		{
			if (incomingPlateImageBlob is null)
			{
				log.LogWarning("Incoming blob was null, unable to process");
				return;
			}

			int incomingByteLength = (int)incomingPlateImageBlob.Length;
			if (incomingByteLength < 1)
			{
				log.LogWarning("Incoming blob had no data (length < 1), unable to process");
				return;
			}

			// Convert the incoming image stream to a byte array.
			byte[] licensePlateImage;

			using (var br = new BinaryReader(incomingPlateImageBlob))
			{
				licensePlateImage = br.ReadBytes(incomingByteLength);
			}

			var processor = new LicensePlateImageProcessor(log);
			var licensePlateText = await processor.GetLicensePlate(licensePlateImage);
			log.LogInformation($"LicensePlateText: {licensePlateText}");

			//TODO: Process the license plate info or send for review
		}
	}
}