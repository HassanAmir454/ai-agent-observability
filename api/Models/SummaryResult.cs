using System.Text.Json.Serialization;

namespace ObservabilityApi.Models;

/// <summary>Aggregated summary response shape per SPEC §5 GET /events/summary.</summary>
public class SummaryResult
{
    [JsonPropertyName("totalEvents")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("timeRange")]
    public string TimeRange { get; set; } = string.Empty;

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("totalCostUsd")]
    public decimal TotalCostUsd { get; set; }

    [JsonPropertyName("totalTokens")]
    public long TotalTokens { get; set; }

    [JsonPropertyName("averageSessionDurationMinutes")]
    public int AverageSessionDurationMinutes { get; set; }

    [JsonPropertyName("byAgent")]
    public List<AgentSummary> ByAgent { get; set; } = new();

    [JsonPropertyName("byActivity")]
    public List<ActivitySummary> ByActivity { get; set; } = new();

    [JsonPropertyName("timeline")]
    public List<TimelineBucket> Timeline { get; set; } = new();

    [JsonPropertyName("recentEvents")]
    public List<AgentEvent> RecentEvents { get; set; } = new();
}

public class AgentSummary
{
    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;

    [JsonPropertyName("eventCount")]
    public int EventCount { get; set; }

    [JsonPropertyName("totalCostUsd")]
    public decimal TotalCostUsd { get; set; }

    [JsonPropertyName("totalTokens")]
    public long TotalTokens { get; set; }
}

public class ActivitySummary
{
    [JsonPropertyName("activityType")]
    public string? ActivityType { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class TimelineBucket
{
    [JsonPropertyName("hour")]
    public DateTime Hour { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("totalCost")]
    public decimal TotalCost { get; set; }
}
