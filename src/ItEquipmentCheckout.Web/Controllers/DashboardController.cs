using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(CheckoutDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        DateTime today = now.Date;
        Dictionary<EquipmentStatus, int> byStatus = await dbContext.Equipment
            .AsNoTracking()
            .GroupBy(item => item.Status)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);
        int employeeCount = await dbContext.Employees.CountAsync(cancellationToken);
        int overdue = await dbContext.Checkouts.CountAsync(
            checkout => checkout.ActualReturnDate == null && checkout.PlannedReturnDate < today,
            cancellationToken);
        List<Checkout> recent = await dbContext.Checkouts
            .AsNoTracking()
            .Include(checkout => checkout.Equipment)
            .Include(checkout => checkout.Employee)
            .OrderByDescending(checkout => checkout.CheckoutDate)
            .Take(5)
            .ToListAsync(cancellationToken);

        int Count(EquipmentStatus status) => byStatus.GetValueOrDefault(status);
        return Ok(new DashboardDto(
            byStatus.Values.Sum(),
            Count(EquipmentStatus.Available),
            Count(EquipmentStatus.CheckedOut),
            Count(EquipmentStatus.Repair),
            Count(EquipmentStatus.Retired),
            employeeCount,
            overdue,
            recent.Select(checkout => checkout.ToDto(now)).ToList()));
    }
}
