namespace ApiTestingPacket.Core.Exceptions;

public sealed class NotFoundException(string message = "The requested resource was not found.") : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class UnauthorizedException(string message = "Authentication is required.") : Exception(message);
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
public sealed class ImportValidationException(IEnumerable<string> errors) : Exception("The imported document is invalid.")
{
    public IReadOnlyList<string> Errors { get; } = errors.ToArray();
}
