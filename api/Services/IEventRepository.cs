using ObservabilityApi.Models;

namespace ObservabilityApi.Services;

public interface IEventRepository
{
    /// <summary>Persists a batch in a single transaction; all or nothing.</summary>
    Task<int> SaveBatchAsync(List<AgentEvent> events);

    /// <summary>Builds the summary aggregate for the given time range.</summary>
    Task<SummaryResult> GetSummaryAsync(string timeRange);

    /// <summary>Returns true if the database can be queried.</summary>
    Task<bool> CheckHealthAsync();
}
