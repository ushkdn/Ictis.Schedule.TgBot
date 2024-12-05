using System.Text.Json.Serialization;

namespace Ictis.Schedule.Data
{
    public record IctisScheduleApiResponse
    {
        [JsonPropertyName("table")]
        public required ScheduleTable Table { get; set; }
    }

    public class ScheduleTable
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("table")]
        public string[,] Table { get; set; } = new string[8, 8];
    }
}