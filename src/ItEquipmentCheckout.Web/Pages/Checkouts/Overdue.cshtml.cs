using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages.Checkouts;

public sealed class OverdueModel(CheckoutDbContext dbContext) : PageModel
{
    public IReadOnlyList<Checkout> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        DateTime today = DateTime.UtcNow.Date;
        Items = await dbContext.Checkouts
            .AsNoTracking()
            .Include(checkout => checkout.Equipment)
            .Include(checkout => checkout.Employee)
            .Where(checkout =>
                checkout.ActualReturnDate == null &&
                checkout.PlannedReturnDate < today)
            .OrderBy(checkout => checkout.PlannedReturnDate)
            .ToListAsync(cancellationToken);
    }
}
