namespace Transaction.Domain.Entities.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
