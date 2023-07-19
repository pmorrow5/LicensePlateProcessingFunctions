// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LicensePlateDataModels;
using System.Net.Http;

namespace LicensePlateProcessingFunctions
{
	public static class ProcessImage
	{
		private static HttpClient _client;

		[FunctionName("ProcessImage")]
		public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, [Blob(blobPath: "{data.url}", access: FileAccess.Read, Connection = "plateImagesStorageConnection")] Stream incomingPlateImageBlob, ILogger log)
		{
			_client = _client ?? new HttpClient();
			log.LogInformation(eventGridEvent.Data.ToString());
			var eventDataInfo = JsonConvert.DeserializeObject<EventDataInfo>(eventGridEvent.Data.ToString());
			
			log.LogInformation($"File: {eventDataInfo.url}");
			log.LogInformation($"contentType: {eventDataInfo.contentType}");
			log.LogInformation($"contentLength: {eventDataInfo.contentLength}");

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
						
			if (eventDataInfo.contentType.ToLower() != "image/jpeg"
				&& eventDataInfo.contentType.ToLower() != "image/png")
			{
				log.LogInformation("Blob content type is not valid for image processing, exiting gracefully");
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

			// Send the details to Event Grid.
			log.LogInformation($"Processing {eventDataInfo.url}");
			await new TriggerEvent(log, _client).SendLicensePlateData(new LicensePlateData()
			{
				FileName = eventDataInfo.url,
				LicensePlateText = licensePlateText,
				TimeStamp = DateTime.UtcNow
			});
		}
	}
}
