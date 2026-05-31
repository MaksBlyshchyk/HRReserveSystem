using HRReserveSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace HRReserveSystem.Services;

public static class DatabaseConfiguration
{
    public const string SqliteProvider = "SQLite";
    public const string PostgresProvider = "PostgreSQL";

    public static string GetProvider(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var provider = configuration["DATABASE_PROVIDER"] ?? configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(provider))
        {
            return NormalizeProvider(provider);
        }

        return SqliteProvider;
    }

    public static string GetConnectionString(IConfiguration configuration, string provider)
    {
        if (IsPostgres(provider))
        {
            var databaseUrl = configuration["DATABASE_URL"];
            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                return ConvertDatabaseUrl(databaseUrl);
            }
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        return connectionString;
    }

    public static bool IsPostgres(string provider)
    {
        return provider.Equals(PostgresProvider, StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase);
    }

    public static void ConfigureDbContext(IServiceCollection services, string provider, string connectionString)
    {
        if (IsPostgres(provider))
        {
            services.AddDbContext<ApplicationDbContext, PostgresApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            return;
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
    }

    private static string NormalizeProvider(string provider)
    {
        if (IsPostgres(provider))
        {
            return PostgresProvider;
        }

        if (provider.Equals(SqliteProvider, StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return SqliteProvider;
        }

        throw new InvalidOperationException(
            $"Unsupported database provider '{provider}'. Use '{SqliteProvider}' or '{PostgresProvider}'.");
    }

    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
        var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};Include Error Detail=false";
    }
}
