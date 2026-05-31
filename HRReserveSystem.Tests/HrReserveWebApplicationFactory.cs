using HRReserveSystem.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRReserveSystem.Tests;

internal sealed class HrReserveWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"HRReserveSystem.Tests-{Guid.NewGuid():N}");
    private string TestConnectionString => $"Data Source={Path.Combine(tempDirectory, "hrreserve-test.db")}";

    public string OutboxPath => Path.Combine(tempDirectory, "EmailOutbox");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(tempDirectory);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SQLite",
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["Email:Enabled"] = "false",
                ["Email:OutboxPath"] = OutboxPath
            });
        });
        builder.ConfigureTestServices(services =>
        {
            var dbContextOptions = services.SingleOrDefault(service => service.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextOptions is not null)
            {
                services.Remove(dbContextOptions);
            }

            var applicationDbContext = services.SingleOrDefault(service => service.ServiceType == typeof(ApplicationDbContext));
            if (applicationDbContext is not null)
            {
                services.Remove(applicationDbContext);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(TestConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(tempDirectory))
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
