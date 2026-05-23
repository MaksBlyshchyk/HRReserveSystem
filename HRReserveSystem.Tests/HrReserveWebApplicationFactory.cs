using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HRReserveSystem.Tests;

internal sealed class HrReserveWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"HRReserveSystem.Tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(tempDirectory);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(tempDirectory, "hrreserve-test.db")}",
                ["Email:Enabled"] = "false",
                ["Email:OutboxPath"] = Path.Combine(tempDirectory, "EmailOutbox")
            });
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
