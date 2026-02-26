using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTeamXPDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<TeamXPDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }

        public static IServiceCollection AddTeamXPRepositories(this IServiceCollection services)
        {
            services.AddScoped<RepositoryUsuarios>();
            services.AddScoped<RepositoryClientes>();
            services.AddScoped<RepositorySeguimiento>();
            services.AddScoped<RepositoryNutricion>();
            services.AddScoped<RepositoryRutinas>();
            services.AddScoped<RepositoryFeedback>();
            services.AddScoped<RepositoryRecursos>();
            return services;
        }

        public static IServiceCollection AddTeamXPCache(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnection = configuration.GetConnectionString("Redis");

            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "TeamXP_";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            return services;
        }
    }
}
