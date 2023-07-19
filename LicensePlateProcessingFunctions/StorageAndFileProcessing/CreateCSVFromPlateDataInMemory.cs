using CsvHelper;
using LicensePlateDataModels;
using LicensePlateProcessingFunctions.CosmosLogic;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LicensePlateProcessingFunctions.StorageAndFileProcessing
{
	internal class CreateCSVFromPlateDataInMemory
	{
		public async Task<byte[]> CreateCSVStreamFromPlateData(IEnumerable<LicensePlateDataDocument> data)
		{
			using (var stream = new MemoryStream())
			{
				using (var textWriter = new StreamWriter(stream))
				{
					using (var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture, false))
					{
						csv.WriteRecords(data.Select(ToLicensePlateData));
						await textWriter.FlushAsync();
						stream.Position = 0;
						var bytes = stream.ToArray();
						return bytes;
					}
				}
			}
		}

		/// <summary>
		/// Used for mapping from a LicensePlateDataDocument object to a LicensePlateData object.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		private static LicensePlateData ToLicensePlateData(LicensePlateDataDocument source)
		{
			return new LicensePlateData
			{
				FileName = source.fileName,
				LicensePlateText = source.licensePlateText,
				TimeStamp = source.timeStamp
			};
		}
	}
}
