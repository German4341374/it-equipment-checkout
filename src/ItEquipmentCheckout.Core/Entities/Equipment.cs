namespace ItEquipmentCheckout.Core.Entities;

public sealed class Equipment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string InventoryNumber { get; set; }

    public EquipmentCategory Category { get; set; }

    public required string Manufacturer { get; set; }

    public required string Model { get; set; }

    public required string SerialNumber { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    public int ExpectedReturnDays { get; set; } = 30;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Checkout> Checkouts { get; } = new List<Checkout>();

    public ICollection<EquipmentEvent> Events { get; } = new List<EquipmentEvent>();
}
