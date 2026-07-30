using ItEquipmentCheckout.Core.Entities;

namespace ItEquipmentCheckout.Web.Contracts;

public sealed record EquipmentEventDto(
    Guid Id,
    Guid EquipmentId,
    string InventoryNumber,
    EquipmentEventType Type,
    string Description,
    DateTime OccurredAt,
    Guid? EmployeeId,
    string? EmployeeName,
    Guid? CheckoutId);

public sealed record DashboardDto(
    int TotalEquipment,
    int Available,
    int CheckedOut,
    int Repair,
    int Retired,
    int Employees,
    int OverdueReturns,
    IReadOnlyList<CheckoutDto> RecentCheckouts);
