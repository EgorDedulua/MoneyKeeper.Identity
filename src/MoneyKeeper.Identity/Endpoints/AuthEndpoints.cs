using AutoMapper;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Extensions;
using MoneyKeeper.Identity.Filters;

namespace MoneyKeeper.Identity.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("api/auth/register", async (RegisterRequest request, IIdentityService identityService, 
                HttpContext httpContext, IMapper mapper, CancellationToken ct) =>
            {
                var result = await identityService.Register(request, ct);

                if (result.IsSuccess)
                {
                    SetRefreshTokenCookie(httpContext, result.Value.RefreshToken);
                    return Results.Ok(mapper.Map<AuthResult>(result.Value));
                }

                return result.ToErrorResult();
            }).WithValidation<RegisterRequest>();

            app.MapPost("api/auth/login", async (LoginRequest request, IIdentityService identityService, 
                HttpContext httpContext, IMapper mapper, CancellationToken ct) =>
            {
                var result = await identityService.Login(request, ct);

                if (result.IsSuccess)
                {
                    SetRefreshTokenCookie(httpContext, result.Value.RefreshToken);
                    return Results.Ok(mapper.Map<AuthResult>(result.Value));
                }

                return result.ToErrorResult();
            }).WithValidation<LoginRequest>();

            app.MapPost("api/auth/refresh", async (IIdentityService identityService, HttpContext httpContext, CancellationToken ct) =>
            {
                string? refreshToken = httpContext.Request.Cookies["refreshToken"];
                var result = await identityService.Refresh(refreshToken, ct);

                if (result.IsSuccess)
                {
                    SetRefreshTokenCookie(httpContext, result.Value.RefreshToken);
                    return Results.Ok(result.Value.AccessToken);
                }

                httpContext.Response.Cookies.Delete("refreshToken");
                return result.ToErrorResult();
            });

            app.MapPost("api/auth/logout", async (IIdentityService identityService, HttpContext httpContext, CancellationToken ct) =>
            {
                string? refreshToken = httpContext.Request.Cookies["refreshToken"];
                await identityService.Logout(refreshToken, ct);
                httpContext.Response.Cookies.Delete("refreshToken");
                return Results.Ok();
            });
        }

        private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
        {
            httpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Strict,
                Path = "api/auth"
            });
        }
    }
}
