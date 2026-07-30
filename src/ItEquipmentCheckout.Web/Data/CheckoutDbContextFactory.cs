using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ItEquipmentCheckout.Web.Data;

public sealed class CheckoutDbContextFactory : IDesignTimeDbContextFactory<CheckoutDbContext>
{
    public CheckoutDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CheckoutDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new CheckoutDbContext(options);
    }
}
