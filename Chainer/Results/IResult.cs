namespace Chainer.Results;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string Error { get; }
    Exception? Exception { get; }
}

public interface IResult<out T> : IResult
{
    T Value { get; }
}