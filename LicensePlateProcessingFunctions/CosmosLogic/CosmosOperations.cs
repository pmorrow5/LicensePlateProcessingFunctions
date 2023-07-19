using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LicensePlateProcessingFunctions.CosmosLogic
{
	internal class CosmosOperations
	{

		private readonly string _endpointUrl;
		private readonly string _authorizationKey;
		private readonly string _databaseId;
		private readonly string _containerId;
		private readonly ILogger _log;
		private static CosmosClient _client;

		public CosmosOperations(string endpointURL, string authorizationKey, string databaseId, string containerId, ILogger log)
		{
			_endpointUrl = endpointURL;
			_authorizationKey = authorizationKey;
			_databaseId = databaseId;
			_containerId = containerId;
			_log = log;
		}

		/// <summary>
		/// Get the license plates that need exported
		/// </summary>
		/// <returns>list of all un-exported license plates</returns>
		public async Task<List<LicensePlateDataDocument>> GetLicensePlatesToExport()
		{
			_log.LogInformation($"Retrieving license plates that are not marked as exported");
			int exportedCount = 0;

			List<LicensePlateDataDocument> licensePlates = new List<LicensePlateDataDocument>();
			if (_client is null) _client = new CosmosClient(_endpointUrl, _authorizationKey);
			var container = _client.GetContainer(_databaseId, _containerId);

			using (FeedIterator<LicensePlateDataDocument> iterator = container.GetItemLinqQueryable<LicensePlateDataDocument>()
					.Where(b => b.exported == false)
					.ToFeedIterator())
			{
				//Asynchronous query execution
				while (iterator.HasMoreResults)
				{
					foreach (var item in await iterator.ReadNextAsync())
					{
						licensePlates.Add(item);
					}
				}
			}

			exportedCount = licensePlates.Count();
			_log.LogInformation($"{exportedCount} license plates found that are ready for export");
			return licensePlates;
		}

		/// <summary>
		/// Update license plate records as exported
		/// </summary>
		/// <param name="licensePlates"></param>
		/// <returns></returns>
		public async Task MarkLicensePlatesAsExported(IEnumerable<LicensePlateDataDocument> licensePlates, bool isConfirmed)
		{
			_log.LogInformation("Updating license plate documents exported values to true");
			if (_client is null) _client = new CosmosClient(_endpointUrl, _authorizationKey);
			var container = _client.GetContainer(_databaseId, _containerId);

			foreach (var licensePlate in licensePlates)
			{
				try
				{
					licensePlate.exported = true;
					licensePlate.confirmed = isConfirmed;
					var response = await container.ReplaceItemAsync(licensePlate, licensePlate.id);
					_log.LogInformation($"Updated {licensePlate.fileName} as exported");
				}
				catch (Exception ex)
				{
					_log.LogError($"Could not update {licensePlate.fileName}: {ex.Message}");
				}
			}
		}
	}
}
