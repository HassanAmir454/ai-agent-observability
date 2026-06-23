using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ObservabilityApi.Functions;

public class Auth
{
    private readonly ILogger<Auth> _logger;

    public Auth(ILogger<Auth> logger)
    {
        _logger = logger;
    }

    [Function("Auth")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/token")] HttpRequest req)
    {
        TokenRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<TokenRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Malformed JSON" });
        }

        if (body is null || string.IsNullOrWhiteSpace(body.ApiKey))
        {
            return new BadRequestObjectResult(new { error = "apiKey is required" });
        }

        var expectedKey = Environment.GetEnvironmentVariable("CollectorApiKey");
        if (string.IsNullOrEmpty(expectedKey) ||
            !string.Equals(body.ApiKey, expectedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejected /auth/token: invalid apiKey.");
            return new UnauthorizedObjectResult(new { error = "Invalid API key" });
        }

        var signingKey = Environment.GetEnvironmentVariable("JwtSigningKey");
        if (string.IsNullOrEmpty(signingKey))
        {
            _logger.LogError("JwtSigningKey is not configured.");
            return new ObjectResult(new { error = "Service temporarily unavailable" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        var expiresAt = DateTime.UtcNow.AddHours(24);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "observability-api",
            audience: "collector",
            claims: [new Claim(ClaimTypes.Role, "collector")],
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new OkObjectResult(new
        {
            token = tokenString,
            expiresAt = expiresAt.ToString("o"),
        });
    }

    private sealed record TokenRequest(string ApiKey);
}
