namespace MoneyKeeper.Identity.Core.Common
{
    public class Error
    {
        public string Message { get; } = string.Empty;

        public int StatusCode { get; }

        public string ErrorCode { get; } = string.Empty;

        public static Error Create(string message, int statusCode, string errorCode)
        {
            if (statusCode < 100 || statusCode > 599)
                throw new ArgumentException("Invalid HTTP status code");
            return new Error(message, statusCode, errorCode);
        }

        private Error(string message, int statusCode, string errorCode)
        {
            Message = message; StatusCode = statusCode; ErrorCode = errorCode;
        }

        public static Error BadRequest(string message, string errorCode)
            => new Error(message, 400, errorCode);

        public static Error Unauthorized(string message, string errorCode)
            => new Error(message, 401, errorCode);

        public static Error Conflict(string message, string errorCode)
           => new Error(message, 409, errorCode);
    }
}
