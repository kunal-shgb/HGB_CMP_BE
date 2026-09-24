namespace ComplaintManagement.Application.Common.Exceptions;

public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

public sealed class ForbiddenAccessException(string message = "You do not have access to this resource.")
    : Exception(message);
