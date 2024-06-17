using CSharpFunctionalExtensions;

namespace ConsoleApp1;

public static class ResultExtensions
{
    public static Result<T> Flatten<T>(this Result<Result<T>> it)
    {
        return it.IsFailure ? Result.Failure<T>(it.Error) : it.Value;
    }
}