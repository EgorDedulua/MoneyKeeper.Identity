using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Extensions;

namespace MoneyKeeper.Identity.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("api/auth/register", async (RegisterRequest request, IIdentityService identityService, CancellationToken ct) =>
            {
                var result = await identityService.Register(request, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return result.ToErrorResult();
            });

            app.MapPost("api/auth/login", async (LoginRequest request, IIdentityService identityService, CancellationToken ct) =>
            {
                var result = await identityService.Login(request, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return result.ToErrorResult();
            });
        }
    }
}
