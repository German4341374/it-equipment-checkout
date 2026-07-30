using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ItEquipmentCheckout.IntegrationTests;

public sealed class CheckoutWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string databasePath =
        Path.Combine(Path.GetTempPath(), $"it-checkout-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CheckoutDatabase"] = $"Data Source={databasePath}",
                ["Database:Seed"] = "false",
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CheckoutDbContext>>();
            services.RemoveAll<CheckoutDbContext>();
            services.AddDbContext<CheckoutDbContext>(options =>
                options.UseSqlite(
                    $"Data Source={databasePath}",
                    sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        DeleteIfExists(databasePath);
        DeleteIfExists($"{databasePath}-shm");
        DeleteIfExists($"{databasePath}-wal");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
