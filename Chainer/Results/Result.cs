namespace Chainer.Results;

public readonly struct Result : IResult
{
    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? string.Empty : error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public override string ToString()
    {
        return IsSuccess
            ? "Success"
            : $"Failure({Error})";
    }

    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> Failure<T>(string error)
    {
        return Result<T>.Failure(error);
    }

    // Implicit conversion from bool to Result
    public static implicit operator Result(bool success)
    {
        return success ? Success() : Failure("Operation failed");
    }

    // Implicit conversion from Exception to Result
    public static implicit operator Result(Exception exception)
    {
        return Failure(exception.Message);
    }

    public Result<T> WithValue<T>(T value)
    {
        return IsSuccess ? Success(value) : Failure<T>(Error);
    }

    public Result Ensure(Func<bool> predicate, string error)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    public Result<T> Ensure<T>(T value, Func<T, bool> predicate, string error)
    {
        if (IsFailure) return Failure<T>(Error);
        return predicate(value) ? Success(value) : Failure<T>(error);
    }

    public Result Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Error);

        return this;
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}