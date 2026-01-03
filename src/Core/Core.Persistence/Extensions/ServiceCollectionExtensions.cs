using Core.Domain.Enumerations;
using Core.Persistence.Context;
using Core.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFramework(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseProvider databaseProvider = DatabaseProvider.PostgreSQL)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string not found for provider {databaseProvider}");
        }

        services.AddScoped<MultiTenantInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            ConfigureDbContextOptions(options, connectionString, databaseProvider);
        });

        return services;
    }

    private static void ConfigureDbContextOptions(
        DbContextOptionsBuilder options,
        string connectionString,
        DatabaseProvider provider)
    {
        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                options.UseNpgsql(connectionString, b =>
                {
                    b.MigrationsAssembly("Core.Persistence");
                    b.EnableRetryOnFailure(3);
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                });

                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                break;

            case DatabaseProvider.MySQL:
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    b =>
                    {
                        b.MigrationsAssembly("Core.Persistence");
                        b.EnableRetryOnFailure(3);
                        b.MigrationsHistoryTable("__EFMigrationsHistory");
                    });
                break;

            case DatabaseProvider.SqlServer:
                options.UseSqlServer(connectionString, b =>
                {
                    b.MigrationsAssembly("Core.Persistence");
                    b.EnableRetryOnFailure(3);
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                });
                break;

            case DatabaseProvider.SQLite:
                options.UseSqlite(connectionString, b =>
                {
                    b.MigrationsAssembly("Core.Persistence");
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                });
                break;

            default:
                throw new NotSupportedException(
                    $"Provider {provider} is not supported " +
                    $"Available providers: PostgreSQL, MySQL, SqlServer, SQLite");
        }
    }
}