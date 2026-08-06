using MoneyKeeper.Identity.Core.Common;

namespace MoneyKeeper.Identity.Extensions
{
    public static class ResultExtensions
    {
        public static IResult ToErrorResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                throw new ArgumentException("ToErrorActionResult called on successful result. " +
                    "Handle success case explicitly with Ok(), Created(), etc.");

            if (result is null)
                throw new ArgumentNullException("ToErrorActionResult called on null result");

            if (result.Error is null)
                throw new ArgumentNullException("ToErrorActionResult called on null Error property of result");

            object error = new 
            {
                error = result.Error.Message,
                code = result.Error.ErrorCode
            };

            return result.Error.StatusCode switch
            {
                400 => Results.BadRequest(error),
                409 => Results.Conflict(error),
                _ => Results.Json(error, statusCode: result.Error.StatusCode)
            };
        }
    }
}
