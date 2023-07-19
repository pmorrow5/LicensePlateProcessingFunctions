using LicensePlateDataModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;

public class TriggerEvent
{
	private readonly HttpClient _client;
	private readonly ILogger _log;

	public TriggerEvent(ILogger log, HttpClient client)
	{
		_log = log;
		_client = client;
	}

	public async Task SendLicensePlateData(LicensePlateData data)
	{
		if (data.LicensePlateFound && !data.NeedsReview)
		{
			await Send("savePlateData", "PlateProcessing/TicketService", data);
		}
		else
		{
			await Send("reviewPlateData", "PlateProcessing/TicketService", data);
		}
	}

	private async Task Send(string eventType, string subject, LicensePlateData data)
	{
		// Get the API URL and the API key from settings.
		var uri = Environment.GetEnvironmentVariable("eventGridTopicEndpoint");
		var key = Environment.GetEnvironmentVariable("eventGridTopicKey");

		_log.LogInformation($"Sending license plate data to the {eventType} Event Grid type");

		var events = new List<Event<LicensePlateData>>
		{
			new Event<LicensePlateData>()
			{
				Data = data,
				EventTime = DateTime.UtcNow,
				EventType = eventType,
				Id = Guid.NewGuid().ToString(),
				Subject = subject
			}
		};

		_client.DefaultRequestHeaders.Clear();
		_client.DefaultRequestHeaders.Add("aeg-sas-key", key);
		await _client.PostAsJsonAsync(uri, events);

		_log.LogInformation($"Sent the following to the Event Grid topic: {events[0]}");
	}
}