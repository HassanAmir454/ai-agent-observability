using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using ObservabilityApi.Services;

namespace ObservabilityApi.Functions;

public class Health
{
    private readonly IEventRepository _repository;

    public Health(IEventRepository repository)
    {
        _repository = repository;
    }

    [Function("Health")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        var healthy = await _repository.CheckHealthAsync();
        if (healthy)
        {
            return new OkObjectResult(new { status = "healthy", database = "connected" });
        }

        return new ObjectResult(new { status = "unhealthy", database = "unreachable" })
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }
}

