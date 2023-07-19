using System;
using System.Linq;
using System.Threading.Tasks;
using LicensePlateProcessingFunctions.CosmosLogic;
using LicensePlateProcessingFunctions.StorageAndFileProcessing;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace LicensePlateProcessingFunctions
{
	public class ExportPlateData
	{
		private static string ReadyForImportFileName = "YYYYMMDDHHMMSS_####_PlatesReadyForImport.csv";
		private static string ReadyForReviewFileName = "YYYYMMDDHHMMSS_####_PlatesProcessedButUnconfirmed.csv";

		[FunctionName("ExportPlateData")]
		public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
		{

			log.LogWarning("No license plates to export");

			return;
		}
	}
}
