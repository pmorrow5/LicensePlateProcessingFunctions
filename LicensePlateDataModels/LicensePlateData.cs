using Newtonsoft.Json;

namespace LicensePlateDataModels
{
    public class LicensePlateData
    {
        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName = "licensePlateText")]
        public string LicensePlateText { get; set; }

        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty(PropertyName = "licensePlateFound")]
        public bool LicensePlateFound => !string.IsNullOrWhiteSpace(LicensePlateText);

        [JsonProperty(PropertyName = "needsReview")]
        public bool NeedsReview => !LicensePlateFound
                    || LicensePlateText.Replace(" ", "").Length < 5
                    || LicensePlateText.Replace(" ", "").Length > 8;
    }
}