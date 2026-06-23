using System.Text.Json.Serialization;

namespace ObservabilityApi.Models;

public class AgentEvent
{
    [JsonIgnore]
    public int Id { get; set; }

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("inputTokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("cacheReadTokens")]
    public int CacheReadTokens { get; set; }

    [JsonPropertyName("cacheWriteTokens")]
    public int CacheWriteTokens { get; set; }

    [JsonPropertyName("reasoningTokens")]
    public int ReasoningTokens { get; set; }

    [JsonPropertyName("totalCostUsd")]
    public decimal TotalCostUsd { get; set; }

    [JsonPropertyName("activityType")]
    public string? ActivityType { get; set; }

    [JsonPropertyName("apiCalls")]
    public int ApiCalls { get; set; } = 1;

    [JsonPropertyName("hasAgentSpawn")]
    public bool HasAgentSpawn { get; set; }

    [JsonPropertyName("sessionDurationMinutes")]
    public int SessionDurationMinutes { get; set; }

    [JsonPropertyName("eventTimestamp")]
    public DateTime EventTimestamp { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonIgnore]
    public DateTime CreatedAt { get; set; }
}
