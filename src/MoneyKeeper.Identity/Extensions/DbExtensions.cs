using Microsoft.EntityFrameworkCore;
using MoneyKeeper.Identity.Infrastructure.Data;

namespace MoneyKeeper.Identity.Extensions
{
    public static class DbExtensions
    {
        public static IServiceCollection AddDb(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static WebApplication MigrateDb(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                context.Database.EnsureCreated();
            }
            return app;
        }
    }
}
