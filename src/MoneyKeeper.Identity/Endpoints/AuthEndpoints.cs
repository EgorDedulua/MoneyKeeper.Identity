using AutoMapper;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Contracts.Auth;
using MoneyKeeper.Identity.Contracts;
using MoneyKeeper.Identity.Extensions;
using MoneyKeeper.Identity.Filters;

namespace MoneyKeeper.Identity.Endpoints
{
    public static class AuthEndpoints
    {
        private const string RefreshTokenCookie = "refreshToken";

        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("api/auth/register", async (RegisterRequest request, IIdentityService identityService, 
                HttpContext httpContext, IMapper mapper, CancellationToken ct) =>
            {
                var result = await identityService.Register(request, ct);

                if (result.IsSuccess)
                {
                    SetRefreshTokenCookie(httpContext, result.Value.RefreshToken);
                    return Results.Ok(mapper.Map<AuthResponse>(result.Value));
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
                    return Results.Ok(mapper.Map<AuthResponse>(result.Value));
                }

                return result.ToErrorResult();
            }).WithValidation<LoginRequest>();

            app.MapPost("api/auth/refresh", async (IIdentityService identityService, HttpContext httpContext, CancellationToken ct) =>
            {
                string? refreshToken = httpContext.Request.Cookies[RefreshTokenCookie];
                var result = await identityService.Refresh(refreshToken, ct);

                if (result.IsSuccess)
                {
                    SetRefreshTokenCookie(httpContext, result.Value.RefreshToken);
                    return Results.Ok(new { accessToken = result.Value.AccessToken });
                }

                return result.ToErrorResult();
            });

            app.MapPost("api/auth/logout", async (IIdentityService identityService, HttpContext httpContext, CancellationToken ct) =>
            {
                string? refreshToken = httpContext.Request.Cookies[RefreshTokenCookie];
                await identityService.Logout(refreshToken, ct);
                RemoveRefreshTokenCookie(httpContext);
                return Results.Ok();
            });
        }

        private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
        {
            RemoveRefreshTokenCookie(httpContext);
            httpContext.Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.None,
                Path = "/api/auth"
            });
        }

        private static void RemoveRefreshTokenCookie(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, 
                Path = "/api/auth"
            });
        }
    }
}
