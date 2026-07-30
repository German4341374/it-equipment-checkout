using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages;

public sealed class IndexModel(CheckoutDbContext dbContext) : PageModel
{
    public int TotalEquipment { get; private set; }

    public int Available { get; private set; }

    public int CheckedOut { get; private set; }

    public int Repair { get; private set; }

    public int Overdue { get; private set; }

    public IReadOnlyList<Checkout> RecentCheckouts { get; private set; } = [];

    public IReadOnlyList<EquipmentEvent> RecentEvents { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dictionary<EquipmentStatus, int> counts = await dbContext.Equipment
            .AsNoTracking()
            .GroupBy(item => item.Status)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);
        TotalEquipment = counts.Values.Sum();
        Available = counts.GetValueOrDefault(EquipmentStatus.Available);
        CheckedOut = counts.GetValueOrDefault(EquipmentStatus.CheckedOut);
        Repair = counts.GetValueOrDefault(EquipmentStatus.Repair);
        DateTime today = DateTime.UtcNow.Date;
        Overdue = await dbContext.Checkouts.CountAsync(
            checkout => checkout.ActualReturnDate == null && checkout.PlannedReturnDate < today,
            cancellationToken);
        RecentCheckouts = await dbContext.Checkouts
            .AsNoTracking()
            .Include(checkout => checkout.Equipment)
            .Include(checkout => checkout.Employee)
            .OrderByDescending(checkout => checkout.CheckoutDate)
            .Take(5)
            .ToListAsync(cancellationToken);
        RecentEvents = await dbContext.EquipmentEvents
            .AsNoTracking()
            .Include(equipmentEvent => equipmentEvent.Equipment)
            .OrderByDescending(equipmentEvent => equipmentEvent.OccurredAt)
            .Take(6)
            .ToListAsync(cancellationToken);
    }
}
