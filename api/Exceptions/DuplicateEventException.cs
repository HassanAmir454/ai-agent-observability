namespace ObservabilityApi.Exceptions;

/// <summary>Thrown when a batch violates the UQ_AgentEvents unique constraint.</summary>
public class DuplicateEventException : Exception
{
    public DuplicateEventException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
