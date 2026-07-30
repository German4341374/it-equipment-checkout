using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ItEquipmentCheckout.Web.Pages.Equipment;

public sealed class CreateModel(CheckoutDbContext dbContext) : PageModel
{
    [BindProperty]
    public CreateEquipmentRequest Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var item = new Core.Entities.Equipment
        {
            InventoryNumber = Input.InventoryNumber,
            Category = Input.Category,
            Manufacturer = Input.Manufacturer,
            Model = Input.Model,
            SerialNumber = Input.SerialNumber,
            ExpectedReturnDays = Input.ExpectedReturnDays,
        };
        item.Events.Add(new EquipmentEvent
        {
            EquipmentId = item.Id,
            Type = EquipmentEventType.Created,
            Description = "Equipment created through the web interface.",
        });
        dbContext.Equipment.Add(item);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Inventory number or serial number already exists.");
            return Page();
        }

        TempData["Success"] = $"{item.InventoryNumber} was added.";
        return RedirectToPage("/Equipment/Details", new { id = item.Id });
    }
}
