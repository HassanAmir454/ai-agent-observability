using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
        if (!TryValidateJwt(req, out var authError))
        {
            _logger.LogWarning("Rejected /events: {Reason}", authError);
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

    private static bool TryValidateJwt(HttpRequest req, out string reason)
    {
        reason = string.Empty;

        if (!req.Headers.TryGetValue("Authorization", out var authHeader))
        {
            reason = "Authorization header missing";
            return false;
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Authorization header must use Bearer scheme";
            return false;
        }

        var tokenString = headerValue["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(tokenString))
        {
            reason = "Bearer token is empty";
            return false;
        }

        var signingKey = Environment.GetEnvironmentVariable("JwtSigningKey");
        if (string.IsNullOrEmpty(signingKey))
        {
            reason = "JwtSigningKey not configured";
            return false;
        }

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "observability-api",
            ValidateAudience = true,
            ValidAudience = "collector",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(tokenString, validationParams, out _);
            return true;
        }
        catch (SecurityTokenException ex)
        {
            reason = ex.Message;
            return false;
        }
    }
}
