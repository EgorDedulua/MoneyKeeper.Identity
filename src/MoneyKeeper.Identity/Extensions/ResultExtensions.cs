using MoneyKeeper.Identity.Core.Common;

namespace MoneyKeeper.Identity.Extensions
{
    public static class ResultExtensions
    {
        public static IResult ToErrorResult<T>(this Result<T> result)
        {
            if (result is null)
                throw new InvalidOperationException("ToErrorActionResult called on null result");

            if (result.IsSuccess)
                throw new InvalidOperationException("ToErrorActionResult called on successful result. " +
                    "Handle success case explicitly with Ok(), Created(), etc.");

            if (result.Error is null)
                throw new InvalidOperationException("ToErrorActionResult called on null Error property of result");

            return Results.Problem(
                title: result.Error.StatusCode.ToErrorTitle(),
                detail: result.Error.Message,
                statusCode: result.Error.StatusCode,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = result.Error.ErrorCode
                }
            );
        }

        private static string ToErrorTitle(this int statusCode) => statusCode switch
        {
            400 => "Bad request",
            401 => "Unauthorized",
            409 => "Conflict",
            _ => "Unexpected error"
        };
    }
}
