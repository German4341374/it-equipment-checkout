using ItEquipmentCheckout.Core.Entities;

namespace ItEquipmentCheckout.Web.Contracts;

public static class DtoMapper
{
    public static EquipmentDto ToDto(this Equipment item, DateTime utcNow)
    {
        Checkout? active = item.Checkouts.SingleOrDefault(checkout => checkout.ActualReturnDate is null);
        CheckoutSummaryDto? activeDto = active is null
            ? null
            : new CheckoutSummaryDto(
                active.Id,
                active.EmployeeId,
                active.Employee.FullName,
                active.CheckoutDate,
                active.PlannedReturnDate,
                active.IsOverdue(utcNow));
        return new EquipmentDto(
            item.Id,
            item.InventoryNumber,
            item.Category,
            item.Manufacturer,
            item.Model,
            item.SerialNumber,
            item.Status,
            item.ExpectedReturnDays,
            activeDto,
            item.CreatedAt,
            item.UpdatedAt);
    }

    public static CheckoutDto ToDto(this Checkout checkout, DateTime utcNow) =>
        new(
            checkout.Id,
            checkout.EquipmentId,
            checkout.Equipment.InventoryNumber,
            $"{checkout.Equipment.Manufacturer} {checkout.Equipment.Model}",
            checkout.EmployeeId,
            checkout.Employee.FullName,
            checkout.CheckoutDate,
            checkout.PlannedReturnDate,
            checkout.ActualReturnDate,
            checkout.IsOverdue(utcNow),
            checkout.Notes);

    public static EmployeeDto ToDto(this Employee employee) =>
        new(
            employee.Id,
            employee.EmployeeNumber,
            employee.FullName,
            employee.Email,
            employee.Department,
            employee.Checkouts.Count(checkout => checkout.ActualReturnDate is null),
            employee.CreatedAt);

    public static EquipmentEventDto ToDto(this EquipmentEvent equipmentEvent) =>
        new(
            equipmentEvent.Id,
            equipmentEvent.EquipmentId,
            equipmentEvent.Equipment.InventoryNumber,
            equipmentEvent.Type,
            equipmentEvent.Description,
            equipmentEvent.OccurredAt,
            equipmentEvent.EmployeeId,
            equipmentEvent.Employee?.FullName,
            equipmentEvent.CheckoutId);
}
