using System.Text.Json.Serialization;

namespace ObservabilityApi.Models;

public class EventBatch
{
    [JsonPropertyName("events")]
    public List<AgentEvent> Events { get; set; } = new();

    [JsonPropertyName("collectorVersion")]
    public string CollectorVersion { get; set; } = string.Empty;
}
