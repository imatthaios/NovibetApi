namespace Novibet.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true);
    public static Result Fail(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data = default, string? error = null)
        : base(isSuccess, error)
    {
        Data = data;
    }

    public static Result<T> Ok(T data) => new(true, data);
    public new static Result<T> Fail(string error) => new(false, default, error);
}