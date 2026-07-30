using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        CheckoutDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Equipment.AnyAsync(cancellationToken))
        {
            return;
        }

        Employee[] employees =
        [
            Employee("1001", "Alex Morgan", "alex.morgan@example.test", "Engineering", 1),
            Employee("1002", "Jordan Lee", "jordan.lee@example.test", "Support", 2),
            Employee("1003", "Sam Rivera", "sam.rivera@example.test", "Finance", 3),
            Employee("1004", "Taylor Kim", "taylor.kim@example.test", "Operations", 4),
            Employee("1005", "Casey Patel", "casey.patel@example.test", "People", 5),
            Employee("1006", "Morgan Chen", "morgan.chen@example.test", "Sales", 6),
        ];

        Equipment[] equipment =
        [
            Item("LT-0001", EquipmentCategory.Laptop, "Framework", "Laptop 13", "DEMO-LT-0001", 30, 101),
            Item("LT-0002", EquipmentCategory.Laptop, "Lenovo", "ThinkPad T14", "DEMO-LT-0002", 21, 102),
            Item("LT-0003", EquipmentCategory.Laptop, "Dell", "Latitude 7450", "DEMO-LT-0003", 30, 103),
            Item("MN-0001", EquipmentCategory.Monitor, "Dell", "UltraSharp 27", "DEMO-MN-0001", 90, 104),
            Item("MN-0002", EquipmentCategory.Monitor, "LG", "27UP850", "DEMO-MN-0002", 90, 105),
            Item("HS-0001", EquipmentCategory.Headset, "Jabra", "Evolve2 65", "DEMO-HS-0001", 60, 106),
            Item("HS-0002", EquipmentCategory.Headset, "Poly", "Voyager Focus", "DEMO-HS-0002", 60, 107),
            Item("DK-0001", EquipmentCategory.DockingStation, "CalDigit", "TS4", "DEMO-DK-0001", 90, 108),
            Item("KB-0001", EquipmentCategory.Keyboard, "Keychron", "K8 Pro", "DEMO-KB-0001", 60, 109),
            Item("MS-0001", EquipmentCategory.Mouse, "Logitech", "MX Master 3S", "DEMO-MS-0001", 60, 110),
            Item("PH-0001", EquipmentCategory.MobilePhone, "Google", "Pixel 9", "DEMO-PH-0001", 30, 111),
            Item("MN-0003", EquipmentCategory.Monitor, "Samsung", "ViewFinity S8", "DEMO-MN-0003", 90, 112),
        ];

        dbContext.Employees.AddRange(employees);
        dbContext.Equipment.AddRange(equipment);
        foreach (Equipment item in equipment)
        {
            item.Events.Add(new EquipmentEvent
            {
                EquipmentId = item.Id,
                Type = EquipmentEventType.Created,
                Description = "Seed equipment created.",
            });
        }

        DateTime today = DateTime.UtcNow.Date;
        EquipmentCheckoutPolicy.CheckOut(
            equipment[0],
            employees[0].Id,
            today.AddDays(-20),
            today.AddDays(-5),
            "Overdue demo checkout");
        EquipmentCheckoutPolicy.CheckOut(
            equipment[3],
            employees[1].Id,
            today.AddDays(-3),
            today.AddDays(11),
            "External monitor");
        Checkout returned = EquipmentCheckoutPolicy.CheckOut(
            equipment[5],
            employees[3].Id,
            today.AddDays(-15),
            today.AddDays(15));
        EquipmentCheckoutPolicy.Return(equipment[5], returned, today.AddDays(-2));
        EquipmentCheckoutPolicy.MarkRepair(equipment[10], "Battery diagnostics");
        EquipmentCheckoutPolicy.Retire(equipment[11], "End of service life");

        dbContext.Checkouts.AddRange(equipment.SelectMany(item => item.Checkouts));
        dbContext.EquipmentEvents.AddRange(equipment.SelectMany(item => item.Events));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Employee Employee(
        string number,
        string name,
        string email,
        string department,
        int id) =>
        new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"),
            EmployeeNumber = number,
            FullName = name,
            Email = email,
            Department = department,
        };

    private static Equipment Item(
        string inventoryNumber,
        EquipmentCategory category,
        string manufacturer,
        string model,
        string serialNumber,
        int expectedReturnDays,
        int id) =>
        new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0001-{id:000000000000}"),
            InventoryNumber = inventoryNumber,
            Category = category,
            Manufacturer = manufacturer,
            Model = model,
            SerialNumber = serialNumber,
            ExpectedReturnDays = expectedReturnDays,
        };
}
