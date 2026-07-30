using ItEquipmentCheckout.Core.Entities;

namespace ItEquipmentCheckout.Web.Ui;

public static class UiText
{
    public static string Status(EquipmentStatus status) => status switch
    {
        EquipmentStatus.CheckedOut => "Checked Out",
        _ => status.ToString(),
    };

    public static string Category(EquipmentCategory category) => category switch
    {
        EquipmentCategory.DockingStation => "Docking Station",
        EquipmentCategory.MobilePhone => "Mobile Phone",
        _ => category.ToString(),
    };

    public static string EventType(EquipmentEventType type) => type switch
    {
        EquipmentEventType.CheckedOut => "Checked Out",
        EquipmentEventType.SentToRepair => "Sent to Repair",
        EquipmentEventType.ReturnedFromRepair => "Returned from Repair",
        _ => type.ToString(),
    };

    public static string StatusClass(EquipmentStatus status) => status switch
    {
        EquipmentStatus.Available => "status-available",
        EquipmentStatus.CheckedOut => "status-checked-out",
        EquipmentStatus.Repair => "status-repair",
        EquipmentStatus.Retired => "status-retired",
        _ => string.Empty,
    };
}
