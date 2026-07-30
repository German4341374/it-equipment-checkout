using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Pages.Employees;

public sealed class IndexModel(CheckoutDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public CreateEmployeeRequest Input { get; set; } = new();

    public IReadOnlyList<Employee> Employees { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var employee = new Employee
        {
            EmployeeNumber = Input.EmployeeNumber,
            FullName = Input.FullName,
            Email = Input.Email,
            Department = Input.Department,
        };
        dbContext.Employees.Add(employee);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Employee number or email already exists.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        TempData["Success"] = $"{employee.FullName} was added.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.Checkouts);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            string pattern = $"%{Search.Trim()}%";
            query = query.Where(employee =>
                EF.Functions.Like(employee.EmployeeNumber, pattern) ||
                EF.Functions.Like(employee.FullName, pattern) ||
                EF.Functions.Like(employee.Email, pattern) ||
                EF.Functions.Like(employee.Department, pattern));
        }

        Employees = await query
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);
    }
}
