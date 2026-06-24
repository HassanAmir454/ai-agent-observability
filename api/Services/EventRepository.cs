using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ObservabilityApi.Data;
using ObservabilityApi.Models;

namespace ObservabilityApi.Services;

public class EventRepository : IEventRepository
{
    private const int UniqueViolation = 2627;
    private const int DuplicateKey = 2601;

    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveBatchAsync(List<AgentEvent> events)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var inserted = 0;

        try
        {
            foreach (var ev in events)
            {
                _db.AgentEvents.Add(ev);
                try
                {
                    await _db.SaveChangesAsync();
                    inserted++;
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
                    && (sql.Number == UniqueViolation || sql.Number == DuplicateKey))
                {
                    // Duplicate — detach the failed entity so EF doesn't retry it,
                    // then continue to the next event without rolling back.
                    _db.Entry(ev).State = EntityState.Detached;
                }
            }

            await transaction.CommitAsync();
            return inserted;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SummaryResult> GetSummaryAsync(string timeRange)
    {
        var cutoff = ResolveCutoff(timeRange);
        var query = _db.AgentEvents
            .AsNoTracking()
            .Where(e => e.EventTimestamp >= cutoff);

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEvents = g.Count(),
                TotalCostUsd = g.Sum(e => e.TotalCostUsd),
                TotalTokens = g.Sum(e => (long)e.InputTokens + e.OutputTokens
                    + e.CacheReadTokens + e.CacheWriteTokens + e.ReasoningTokens),
                AverageSessionDurationMinutes = g.Average(e => (double?)e.SessionDurationMinutes)
            })
            .FirstOrDefaultAsync();

        var byAgent = await query
            .GroupBy(e => e.AgentName)
            .Select(g => new AgentSummary
            {
                AgentName = g.Key,
                EventCount = g.Count(),
                TotalCostUsd = g.Sum(e => e.TotalCostUsd),
                TotalTokens = g.Sum(e => (long)e.InputTokens + e.OutputTokens
                    + e.CacheReadTokens + e.CacheWriteTokens + e.ReasoningTokens)
            })
            .OrderByDescending(a => a.EventCount)
            .ToListAsync();

        var byActivity = await query
            .GroupBy(e => e.ActivityType)
            .Select(g => new ActivitySummary
            {
                ActivityType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(a => a.Count)
            .ToListAsync();

        var rawTimeline = await query
            .GroupBy(e => new
            {
                e.EventTimestamp.Year,
                e.EventTimestamp.Month,
                e.EventTimestamp.Day,
                e.EventTimestamp.Hour
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                Count = g.Count(),
                TotalCost = g.Sum(e => e.TotalCostUsd)
            })
            .ToListAsync();

        var timeline = rawTimeline
            .Select(t => new TimelineBucket
            {
                Hour = new DateTime(t.Year, t.Month, t.Day, t.Hour, 0, 0, DateTimeKind.Utc),
                Count = t.Count,
                TotalCost = t.TotalCost
            })
            .OrderBy(t => t.Hour)
            .ToList();

        var recentEvents = await query
            .OrderByDescending(e => e.EventTimestamp)
            .Take(20)
            .ToListAsync();

        return new SummaryResult
        {
            TotalEvents = totals?.TotalEvents ?? 0,
            TimeRange = timeRange,
            GeneratedAt = DateTime.UtcNow,
            TotalCostUsd = totals?.TotalCostUsd ?? 0m,
            TotalTokens = totals?.TotalTokens ?? 0L,
            AverageSessionDurationMinutes = (int)Math.Round(totals?.AverageSessionDurationMinutes ?? 0d),
            ByAgent = byAgent,
            ByActivity = byActivity,
            Timeline = timeline,
            RecentEvents = recentEvents
        };
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            return await _db.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Maps a time-range token to its UTC cutoff; throws on invalid input.</summary>
    private static DateTime ResolveCutoff(string timeRange)
    {
        var now = DateTime.UtcNow;
        return timeRange switch
        {
            "1h" => now.AddHours(-1),
            "6h" => now.AddHours(-6),
            "24h" => now.AddHours(-24),
            "7d" => now.AddDays(-7),
            _ => throw new ArgumentException(
                "Invalid timeRange. Valid options: 1h, 6h, 24h, 7d.", nameof(timeRange))
        };
    }
}
