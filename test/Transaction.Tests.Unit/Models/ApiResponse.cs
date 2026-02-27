namespace Transaction.Tests.Unit.Models;

public sealed class ApiResponse<T>
{
    public int StatusCode { get; set; }

    public bool IsError { get; set; }

    public T? Result { get; set; }

    public string? ErrorBody { get; set; }
}
