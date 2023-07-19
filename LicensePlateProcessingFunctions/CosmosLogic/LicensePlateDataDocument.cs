using System;
namespace LicensePlateProcessingFunctions.CosmosLogic
{
	public class LicensePlateDataDocument
	{
		public string fileName { get; set; }
		public string licensePlateText { get; set; }
		public DateTime timeStamp { get; set; }
		public bool licensePlateFound { get; set; }
		public bool exported { get; set; }
		public bool confirmed { get; set; }
		public string id { get; set; }
		public string _eTag { get; set; }
		public string _rid { get; set; }
		public string _self { get; set; }
		public string _attachments { get; set; }
		public string _ts { get; set; }
	}
}
