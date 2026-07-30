using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages.Equipment;

public sealed class IndexModel(
    CheckoutDbContext dbContext,
    CsvEquipmentService csvService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public EquipmentCategory? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public EquipmentStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Overdue { get; set; }

    [BindProperty]
    public IFormFile? ImportFile { get; set; }

    public IReadOnlyList<Core.Entities.Equipment> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Items = await BuildQuery().ToListAsync(cancellationToken);

    public async Task<IActionResult> OnPostImportAsync(CancellationToken cancellationToken)
    {
        if (ImportFile is null || ImportFile.Length is <= 0 or > 2 * 1024 * 1024)
        {
            TempData["Error"] = "Choose a CSV file smaller than 2 MiB.";
            return RedirectToPage();
        }

        await using Stream stream = ImportFile.OpenReadStream();
        CsvImportResult result = await csvService.ImportAsync(stream, cancellationToken);
        TempData[result.Rejected == 0 ? "Success" : "Error"] =
            $"Imported {result.Imported} item(s); rejected {result.Rejected} row(s).";
        return RedirectToPage();
    }

    private IQueryable<Core.Entities.Equipment> BuildQuery()
    {
        IQueryable<Core.Entities.Equipment> query = dbContext.Equipment
            .AsNoTracking()
            .Include(item => item.Checkouts.Where(checkout => checkout.ActualReturnDate == null))
                .ThenInclude(checkout => checkout.Employee);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            string pattern = $"%{Search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.Like(item.InventoryNumber, pattern) ||
                EF.Functions.Like(item.SerialNumber, pattern) ||
                EF.Functions.Like(item.Manufacturer, pattern) ||
                EF.Functions.Like(item.Model, pattern) ||
                item.Checkouts.Any(checkout =>
                    checkout.ActualReturnDate == null &&
                    EF.Functions.Like(checkout.Employee.FullName, pattern)));
        }

        if (Category.HasValue)
        {
            query = query.Where(item => item.Category == Category.Value);
        }

        if (Status.HasValue)
        {
            query = query.Where(item => item.Status == Status.Value);
        }

        if (Overdue)
        {
            DateTime today = DateTime.UtcNow.Date;
            query = query.Where(item => item.Checkouts.Any(checkout =>
                checkout.ActualReturnDate == null &&
                checkout.PlannedReturnDate < today));
        }

        return query.OrderBy(item => item.InventoryNumber);
    }
}
