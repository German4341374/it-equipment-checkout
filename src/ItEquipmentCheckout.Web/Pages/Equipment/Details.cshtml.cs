using System.ComponentModel.DataAnnotations;
using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Core.Services;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages.Equipment;

public sealed class DetailsModel(
    CheckoutDbContext dbContext,
    CheckoutApplicationService checkoutService) : PageModel
{
    public Core.Entities.Equipment Item { get; private set; } = null!;

    public IReadOnlyList<Employee> Employees { get; private set; } = [];

    public Checkout? ActiveCheckout =>
        Item.Checkouts.SingleOrDefault(checkout => checkout.ActualReturnDate is null);

    [BindProperty]
    public CheckoutInput CheckoutForm { get; set; } = new();

    [BindProperty]
    public DateTime ReturnDate { get; set; } = DateTime.UtcNow.Date;

    [BindProperty]
    [StringLength(300)]
    public string? Reason { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        await LoadAsync(id, cancellationToken);
        CheckoutForm.PlannedReturnDate = DateTime.UtcNow.Date.AddDays(Item.ExpectedReturnDays);
        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        try
        {
            await checkoutService.CheckOutAsync(
                id,
                CheckoutForm.EmployeeId,
                DateTime.UtcNow,
                CheckoutForm.PlannedReturnDate,
                CheckoutForm.Notes,
                cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(id, cancellationToken);
            return Page();
        }

        TempData["Success"] = "Equipment was checked out.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReturnAsync(
        Guid id,
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        try
        {
            await checkoutService.ReturnAsync(checkoutId, ReturnDate, cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToPage(new { id });
        }

        TempData["Success"] = "Equipment was returned and is available.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRepairAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await checkoutService.MarkRepairAsync(id, Reason, cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAvailableAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await checkoutService.MakeAvailableAsync(id, cancellationToken);
            TempData["Success"] = "Equipment returned from repair.";
        }
        catch (BusinessRuleException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRetireAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await checkoutService.RetireAsync(id, Reason, cancellationToken);
            TempData["Success"] = "Equipment retired.";
        }
        catch (BusinessRuleException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        Item = await dbContext.Equipment
            .AsNoTracking()
            .Include(item => item.Checkouts)
                .ThenInclude(checkout => checkout.Employee)
            .Include(item => item.Events)
                .ThenInclude(equipmentEvent => equipmentEvent.Employee)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Equipment", id);
        Employees = await dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);
    }

    public sealed class CheckoutInput
    {
        [Required]
        public Guid EmployeeId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime PlannedReturnDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
