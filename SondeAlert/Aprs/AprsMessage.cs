using System.Text.Json.Serialization;

namespace ElectricFox.SondeAlert.Aprs
{
    public class AprsMessage
    {
        [JsonPropertyName("messageid")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("srccall")]
        public string SourceCallsign { get; set; } = string.Empty;

        [JsonPropertyName("dst")]
        public string DestinationCallsign { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class AprsMessageResponse
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("found")]
        public int? Found { get; set; }

        [JsonPropertyName("what")]
        public string What { get; set; } = string.Empty;

        [JsonPropertyName("entries")]
        public List<AprsMessage> Messages { get; set; }
    }
}
