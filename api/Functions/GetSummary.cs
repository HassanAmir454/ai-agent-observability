using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ObservabilityApi.Services;

namespace ObservabilityApi.Functions;

public class GetSummary
{
    private static readonly HashSet<string> ValidRanges = new() { "1h", "6h", "24h", "7d" };

    private readonly IEventRepository _repository;
    private readonly ILogger<GetSummary> _logger;

    public GetSummary(IEventRepository repository, ILogger<GetSummary> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("GetSummary")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/summary")] HttpRequest req)
    {
        var timeRange = req.Query["timeRange"].ToString();
        if (string.IsNullOrEmpty(timeRange))
        {
            timeRange = "24h";
        }

        if (!ValidRanges.Contains(timeRange))
        {
            return new BadRequestObjectResult(new
            {
                error = "Invalid timeRange. Valid options: 1h, 6h, 24h, 7d."
            });
        }

        try
        {
            var summary = await _repository.GetSummaryAsync(timeRange);
            return new OkObjectResult(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build summary.");
            return new ObjectResult(new { error = "Service temporarily unavailable" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }
}
