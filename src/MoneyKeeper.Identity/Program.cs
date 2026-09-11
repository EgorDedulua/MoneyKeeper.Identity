using MoneyKeeper.Identity.Endpoints;
using MoneyKeeper.Identity.ExceptionHandlers;
using MoneyKeeper.Identity.Extensions;
using MoneyKeeper.Identity.Middlewares;
using Serilog;

namespace MoneyKeeper.Identity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddConfigurations(builder.Configuration);
            builder.Services.AddServices();
            builder.Services.AddRabbitMq(builder.Configuration);
            builder.Services.AddOutbox();
            builder.UseAppLogging();
            builder.Services.AddDb(builder.Configuration);
            builder.Services.AddOpenApi();
            builder.Services.AddAppHealthChecks();
            builder.Services.AddAppAuthorization(builder.Configuration);
            var app = builder.Build();
            app.MigrateDb();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseExceptionHandler();
            app.UseMiddleware<LogContextEnrichmentMiddleware>();
            app.UseAppRequestLogging();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapAppHealthChecks();
            app.MapAuthEndpoints();
            app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);
            app.Run();
        }
    }
}
