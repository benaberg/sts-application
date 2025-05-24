using System.Text.Json.Serialization;

namespace STSApplication.model
{
    internal class TemperatureReading
    {
        [JsonPropertyName("temperature")]
        public required int Temperature { get; set; }

        [JsonPropertyName("timestamp")]
        public required long Timestamp { get; set; }

    }
}
