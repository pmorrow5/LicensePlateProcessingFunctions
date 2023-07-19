using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly.CircuitBreaker;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class LicensePlateImageProcessor
{
	private readonly ILogger _log;

	public LicensePlateImageProcessor(ILogger log)
	{
		_log = log;
	}

	public async Task<string> GetLicensePlate(byte[] imageBytes)
	{
		_log.LogInformation("Making OCR request");
		var licensePlate = string.Empty;

		var visionEndpoint = Environment.GetEnvironmentVariable("computerVisionUrl");
		var visionKey = Environment.GetEnvironmentVariable("computerVisionKey");
		_log.LogInformation($"Retrieved key for endpoint ${visionEndpoint}");

		try
		{
			var client = GetClient(visionEndpoint, visionKey);

			// Analyze the URL image 
			var imgStream = new MemoryStream(imageBytes);

			var ocrResult = await client.RecognizePrintedTextInStreamAsync(detectOrientation: true, image: imgStream);

			var resultData = JsonConvert.SerializeObject(ocrResult);
			_log.LogInformation($"result: {resultData}");

			licensePlate = GetLicensePlateTextFromResult(ocrResult);
			_log.LogInformation($"LicensePlate Found: {licensePlate}");
		}
		catch (BrokenCircuitException bce)
		{
			_log.LogError($"Could not contact the Computer Vision API service due to the following error: {bce.Message}");
		}
		catch (Exception e)
		{
			_log.LogError($"Critical error: {e.Message}", e);
		}

		return licensePlate;
	}

	private ComputerVisionClient GetClient(string endpoint, string key)
	{
		var creds = new ApiKeyServiceClientCredentials(key);

		return new ComputerVisionClient(creds) { Endpoint = endpoint };
	}

	private string GetLicensePlateTextFromResult(OcrResult result)
	{
		var text = string.Empty;
		if (result.Regions == null || result.Regions.Count == 0) return string.Empty;

		const string states = "ALABAMA,ALASKA,ARIZONA,ARKANSAS,CALIFORNIA,COLORADO,CONNECTICUT,DELAWARE,FLORIDA,GEORGIA,HAWAII,IDAHO,ILLINOIS,INDIANA,IOWA,KANSAS,KENTUCKY,LOUISIANA,MAINE,MARYLAND,MASSACHUSETTS,MICHIGAN,MINNESOTA,MISSISSIPPI,MISSOURI,MONTANA,NEBRASKA,NEVADA,NEW HAMPSHIRE,NEW JERSEY,NEW MEXICO,NEW YORK,NORTH CAROLINA,NORTH DAKOTA,OHIO,OKLAHOMA,OREGON,PENNSYLVANIA,RHODE ISLAND,SOUTH CAROLINA,SOUTH DAKOTA,TENNESSEE,TEXAS,UTAH,VERMONT,VIRGINIA,WASHINGTON,WEST VIRGINIA,WISCONSIN,WYOMING";
		string[] chars = { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "_", "(", ")", ":", "|", "[", "]" };
		var stateList = states.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

		// We are only interested in the first region found, and only the first two lines within the region.
		foreach (var line in result.Regions[0].Lines.Take(2))
		{
			_log.LogInformation($"region line: {line}");

			// Exclude the state name.
			if (stateList.Contains(line.Words[0].Text.ToUpper())) continue;

			// TODO: Fix this logic for plate detection
			foreach (var word in line.Words)
			{
				if (string.IsNullOrWhiteSpace(word.Text)) continue;

				text += (RemoveSpecialCharacters(word.Text)) + " "; // Spaces are valid in a license plate.
			}
		}

		_log.LogInformation($"Plate Data: {text}");
		return text.ToUpper().Trim();
	}

	private string RemoveSpecialCharacters(string str)
	{
		var buffer = new char[str.Length];
		int idx = 0;

		foreach (var c in str)
		{
			if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
				|| (c >= 'a' && c <= 'z') || (c == '-'))
			{
				buffer[idx] = c;
				idx++;
			}
		}

		return new string(buffer, 0, idx);
	}
}
