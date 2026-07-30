using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages.Events;

public sealed class IndexModel(CheckoutDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public EquipmentEventType? Type { get; set; }

    public IReadOnlyList<EquipmentEvent> Events { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IQueryable<EquipmentEvent> query = dbContext.EquipmentEvents
            .AsNoTracking()
            .Include(equipmentEvent => equipmentEvent.Equipment)
            .Include(equipmentEvent => equipmentEvent.Employee);
        if (Type.HasValue)
        {
            query = query.Where(equipmentEvent => equipmentEvent.Type == Type.Value);
        }

        Events = await query
            .OrderByDescending(equipmentEvent => equipmentEvent.OccurredAt)
            .Take(250)
            .ToListAsync(cancellationToken);
    }
}
