using ItEquipmentCheckout.Core.Entities;

namespace ItEquipmentCheckout.Core.Services;

public static class EquipmentCheckoutPolicy
{
    public static Checkout CheckOut(
        Equipment equipment,
        Guid employeeId,
        DateTime checkoutDate,
        DateTime? plannedReturnDate,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        if (equipment.Status != EquipmentStatus.Available)
        {
            throw new BusinessRuleException(
                "equipment_not_available",
                $"Equipment in {equipment.Status} status cannot be checked out.");
        }

        if (equipment.Checkouts.Any(checkout => checkout.ActualReturnDate is null))
        {
            throw new BusinessRuleException(
                "equipment_already_checked_out",
                "Equipment already has an active checkout.");
        }

        DateTime normalizedCheckoutDate = NormalizeDate(checkoutDate);
        DateTime normalizedPlannedReturnDate = NormalizeDate(
            plannedReturnDate ?? normalizedCheckoutDate.AddDays(equipment.ExpectedReturnDays));
        if (normalizedPlannedReturnDate < normalizedCheckoutDate)
        {
            throw new BusinessRuleException(
                "invalid_planned_return_date",
                "Planned return date cannot be before checkout date.");
        }

        var checkout = new Checkout
        {
            EquipmentId = equipment.Id,
            EmployeeId = employeeId,
            CheckoutDate = normalizedCheckoutDate,
            PlannedReturnDate = normalizedPlannedReturnDate,
            Notes = notes?.Trim() ?? string.Empty,
        };
        equipment.Checkouts.Add(checkout);
        equipment.Status = EquipmentStatus.CheckedOut;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.Events.Add(new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            EmployeeId = employeeId,
            CheckoutId = checkout.Id,
            Type = EquipmentEventType.CheckedOut,
            Description = $"Checked out until {normalizedPlannedReturnDate:yyyy-MM-dd}.",
        });

        return checkout;
    }

    public static void Return(Equipment equipment, Checkout checkout, DateTime actualReturnDate)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(checkout);

        if (checkout.EquipmentId != equipment.Id || checkout.ActualReturnDate is not null)
        {
            throw new BusinessRuleException(
                "checkout_not_active",
                "The selected checkout is not active for this equipment.");
        }

        DateTime normalizedReturnDate = NormalizeDate(actualReturnDate);
        if (normalizedReturnDate < checkout.CheckoutDate)
        {
            throw new BusinessRuleException(
                "invalid_actual_return_date",
                "Actual return date cannot be before checkout date.");
        }

        checkout.ActualReturnDate = normalizedReturnDate;
        equipment.Status = EquipmentStatus.Available;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.Events.Add(new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            EmployeeId = checkout.EmployeeId,
            CheckoutId = checkout.Id,
            Type = EquipmentEventType.Returned,
            Description = $"Returned on {normalizedReturnDate:yyyy-MM-dd}.",
        });
    }

    public static void MarkRepair(Equipment equipment, string? reason)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        if (equipment.Status == EquipmentStatus.CheckedOut ||
            equipment.Checkouts.Any(checkout => checkout.ActualReturnDate is null))
        {
            throw new BusinessRuleException(
                "active_checkout_exists",
                "Checked-out equipment must be returned before repair.");
        }

        if (equipment.Status == EquipmentStatus.Retired)
        {
            throw new BusinessRuleException(
                "equipment_retired",
                "Retired equipment cannot be moved to repair.");
        }

        equipment.Status = EquipmentStatus.Repair;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.Events.Add(new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            Type = EquipmentEventType.SentToRepair,
            Description = string.IsNullOrWhiteSpace(reason)
                ? "Marked for repair."
                : $"Marked for repair: {reason.Trim()}",
        });
    }

    public static void MakeAvailable(Equipment equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (equipment.Status != EquipmentStatus.Repair)
        {
            throw new BusinessRuleException(
                "equipment_not_in_repair",
                "Only equipment in repair can be returned to available status.");
        }

        equipment.Status = EquipmentStatus.Available;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.Events.Add(new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            Type = EquipmentEventType.ReturnedFromRepair,
            Description = "Returned from repair.",
        });
    }

    public static void Retire(Equipment equipment, string? reason)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (equipment.Status == EquipmentStatus.CheckedOut ||
            equipment.Checkouts.Any(checkout => checkout.ActualReturnDate is null))
        {
            throw new BusinessRuleException(
                "active_checkout_exists",
                "Checked-out equipment must be returned before retirement.");
        }

        equipment.Status = EquipmentStatus.Retired;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.Events.Add(new EquipmentEvent
        {
            EquipmentId = equipment.Id,
            Type = EquipmentEventType.Retired,
            Description = string.IsNullOrWhiteSpace(reason)
                ? "Equipment retired."
                : $"Equipment retired: {reason.Trim()}",
        });
    }

    private static DateTime NormalizeDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
