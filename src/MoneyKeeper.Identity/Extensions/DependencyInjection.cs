using FluentValidation;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Application.Services;
using MoneyKeeper.Identity.Core.Common.Interfaces;
using MoneyKeeper.Identity.Infrastructure.Auth;
using MoneyKeeper.Identity.Infrastructure.Data.Repositories;
using MoneyKeeper.Identity.Profiles;
using MoneyKeeper.Identity.Validators;
using System.Diagnostics;

namespace MoneyKeeper.Identity.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IUsersRepository, UsersRepository>();
            services.AddScoped<IRefreshTokensRepository, RefreshTokensRepository>();
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<AuthResponseProfile>();
            });
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
            services.AddEndpointsApiExplorer();
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions["traceId"] =
                        Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

                    if (context.Exception is not null && !context.ProblemDetails.Extensions.ContainsKey("errorCode"))
                    {
                        context.ProblemDetails.Extensions["errorCode"] = "UNHANDLED_EXCEPTION";
                    }
                };
            });
            return services;
        }

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
            return services;
        }
    }
}
