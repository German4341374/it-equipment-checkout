using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(CheckoutDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EquipmentEventDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentEventDto>>> GetAll(
        [FromQuery] Guid? equipmentId,
        [FromQuery] EquipmentEventType? type,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            ModelState.AddModelError(nameof(limit), "Limit must be between 1 and 500.");
            return ValidationProblem(ModelState);
        }

        IQueryable<EquipmentEvent> query = dbContext.EquipmentEvents
            .AsNoTracking()
            .Include(equipmentEvent => equipmentEvent.Equipment)
            .Include(equipmentEvent => equipmentEvent.Employee);
        if (equipmentId.HasValue)
        {
            query = query.Where(equipmentEvent => equipmentEvent.EquipmentId == equipmentId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(equipmentEvent => equipmentEvent.Type == type.Value);
        }

        List<EquipmentEvent> events = await query
            .OrderByDescending(equipmentEvent => equipmentEvent.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return Ok(events.Select(equipmentEvent => equipmentEvent.ToDto()).ToList());
    }
}
