using FluentValidation;

namespace MoneyKeeper.Identity.Filters
{
    public static class ValidationFilter
    {
        public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        {
            builder.AddEndpointFilter(async (context, next) =>
            {
                var request = context.Arguments.OfType<T>().FirstOrDefault();
                if (request is null)
                    throw new InvalidOperationException(
                        $"Validation filter for {typeof(T).Name} failed: " +
                        $"no parameter of type {typeof(T).Name} was found in the endpoint handler. " +
                        $"Make sure the endpoint has a parameter of type {typeof(T).Name} decorated with [FromBody].");

                var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
                if (validator is not null)
                {
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors
                            .Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage, ErrorCode = e.ErrorCode });
                        return Results.BadRequest(errors);
                    }
                }

                return await next(context);
            });

            return builder;
        }
    }
}
