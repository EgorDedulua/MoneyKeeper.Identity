namespace MoneyKeeper.Identity.Core.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; init; }

        public T Value { get; init; }

        public Error? Error { get; init; }

        public static Result<T> Success(T value) => new(value);

        public static Result<T> Failure(Error error) => new(default!, false, error);

        private Result(T value, bool isSuccess = true, Error? error = null)
        {
            Value = value; IsSuccess = isSuccess; Error = error;
        }
    }
}
