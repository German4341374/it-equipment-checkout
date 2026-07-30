using ItEquipmentCheckout.Web.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ItEquipmentCheckout.Web.Services;

public sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CheckoutDbContext>();
        bool available = await dbContext.Database.CanConnectAsync(cancellationToken);
        return available
            ? HealthCheckResult.Healthy("SQLite database is available.")
            : HealthCheckResult.Unhealthy("SQLite database is unavailable.");
    }
}
