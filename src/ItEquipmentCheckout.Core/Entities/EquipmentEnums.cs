namespace ItEquipmentCheckout.Core.Entities;

public enum EquipmentCategory
{
    Laptop,
    Monitor,
    Headset,
    Keyboard,
    Mouse,
    DockingStation,
    MobilePhone,
    Other
}

public enum EquipmentStatus
{
    Available,
    CheckedOut,
    Repair,
    Retired
}

public enum EquipmentEventType
{
    Created,
    Updated,
    CheckedOut,
    Returned,
    SentToRepair,
    ReturnedFromRepair,
    Retired,
    Imported
}
