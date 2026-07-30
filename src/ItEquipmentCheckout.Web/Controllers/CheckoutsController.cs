using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Controllers;

[ApiController]
[Route("api/checkouts")]
public sealed class CheckoutsController(
    CheckoutDbContext dbContext,
    CheckoutApplicationService checkoutService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CheckoutDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckoutDto>>> GetAll(
        [FromQuery] bool? active,
        [FromQuery] bool overdue = false,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        IQueryable<Checkout> query = dbContext.Checkouts
            .AsNoTracking()
            .Include(checkout => checkout.Equipment)
            .Include(checkout => checkout.Employee);
        if (active.HasValue)
        {
            query = active.Value
                ? query.Where(checkout => checkout.ActualReturnDate == null)
                : query.Where(checkout => checkout.ActualReturnDate != null);
        }

        if (overdue)
        {
            DateTime today = now.Date;
            query = query.Where(checkout =>
                checkout.ActualReturnDate == null &&
                checkout.PlannedReturnDate < today);
        }

        List<Checkout> checkouts = await query
            .OrderByDescending(checkout => checkout.CheckoutDate)
            .ToListAsync(cancellationToken);
        return Ok(checkouts.Select(checkout => checkout.ToDto(now)).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CheckoutDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Checkout checkout = await dbContext.Checkouts
            .AsNoTracking()
            .Include(item => item.Equipment)
            .Include(item => item.Employee)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Checkout", id);
        return Ok(checkout.ToDto(DateTime.UtcNow));
    }

    [HttpPost]
    [ProducesResponseType<CheckoutDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutDto>> Create(
        CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        Checkout checkout = await checkoutService.CheckOutAsync(
            request.EquipmentId,
            request.EmployeeId,
            request.CheckoutDate ?? DateTime.UtcNow,
            request.PlannedReturnDate,
            request.Notes,
            cancellationToken);
        CheckoutDto response = checkout.ToDto(DateTime.UtcNow);
        return CreatedAtAction(nameof(GetById), new { id = checkout.Id }, response);
    }

    [HttpPost("{id:guid}/return")]
    [ProducesResponseType<CheckoutDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutDto>> Return(
        Guid id,
        ReturnCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        Checkout checkout = await checkoutService.ReturnAsync(
            id,
            request.ActualReturnDate ?? DateTime.UtcNow,
            cancellationToken);
        return Ok(checkout.ToDto(DateTime.UtcNow));
    }
}
