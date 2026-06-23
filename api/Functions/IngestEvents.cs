using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ObservabilityApi.Exceptions;
using ObservabilityApi.Models;
using ObservabilityApi.Services;

namespace ObservabilityApi.Functions;

public class IngestEvents
{
    private const int MaxBatchSize = 1000;

    private readonly IEventRepository _repository;
    private readonly ILogger<IngestEvents> _logger;

    public IngestEvents(IEventRepository repository, ILogger<IngestEvents> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("IngestEvents")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequest req)
    {
        var expectedKey = Environment.GetEnvironmentVariable("CollectorApiKey");
        req.Headers.TryGetValue("X-Api-Key", out var providedKey);

        if (string.IsNullOrEmpty(expectedKey) ||
            !string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejected /events: missing or invalid X-Api-Key.");
            return new UnauthorizedObjectResult(new { error = "Unauthorized" });
        }

        EventBatch? batch;
        try
        {
            batch = await JsonSerializer.DeserializeAsync<EventBatch>(req.Body);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Malformed JSON" });
        }

        if (batch?.Events == null || batch.Events.Count == 0)
        {
            return new BadRequestObjectResult(new { error = "Batch cannot be empty" });
        }

        if (batch.Events.Count > MaxBatchSize)
        {
            return new BadRequestObjectResult(new { error = "Batch cannot exceed 1000 events" });
        }

        for (var i = 0; i < batch.Events.Count; i++)
        {
            var ev = batch.Events[i];
            if (string.IsNullOrWhiteSpace(ev.AgentName))
            {
                return new BadRequestObjectResult(
                    new { error = $"events[{i}].agentName is required" });
            }

            if (ev.EventTimestamp == default)
            {
                return new BadRequestObjectResult(
                    new { error = $"events[{i}].eventTimestamp is invalid" });
            }
        }

        try
        {
            var stored = await _repository.SaveBatchAsync(batch.Events);
            return new ObjectResult(new { stored, message = "Events stored" })
            {
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (DuplicateEventException ex)
        {
            _logger.LogInformation(ex, "Duplicate batch ignored (idempotent retry).");
            return new ObjectResult(new
            {
                stored = 0,
                message = "Batch already received; no events re-inserted"
            })
            {
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store events batch.");
            return new ObjectResult(new { error = "Service temporarily unavailable" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }
    }
}
