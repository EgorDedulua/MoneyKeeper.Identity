using MoneyKeeper.Identity.Endpoints;
using MoneyKeeper.Identity.Extensions;

namespace MoneyKeeper.Identity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddServices();
            builder.Services.AddConfigurations(builder.Configuration);
            builder.Services.AddDb(builder.Configuration);
            builder.Services.AddOpenApi();
            builder.Services.AddAppHealthChecks();
            var app = builder.Build();
            app.MigrateDb();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapAppHealthChecks();
            app.MapAuthEndpoints();
            app.Run();
        }
    }
}
