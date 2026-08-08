using FluentValidation;

namespace MoneyKeeper.Identity.Filters
{
    public static class ValidationFilter
    {
        public static RouteHandlerBuilder WithValidation<T> (this RouteHandlerBuilder builder)
        {
            builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request is null)
                    throw new InvalidOperationException(
                        $"Validation filter for {typeof(T).Name} failed: " +
                        $"no parameter of type {typeof(T).Name} was found in the endpoint handler.");

                var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
                if (validator is not null)
                {
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                        var errorCodes = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorCode).ToArray());

                        return Results.ValidationProblem(
                            title: "Validation error",
                            errors: errors,
                            extensions: new Dictionary<string, object?>
                            {
                                ["errorCodes"] = errorCodes
                            }
                        );
                    }
                }

                return await next(context);
            });
            return builder;
        }
    }
}
