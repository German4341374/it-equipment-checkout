namespace ItEquipmentCheckout.Core.Entities;

public sealed class EquipmentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EquipmentId { get; set; }

    public Equipment Equipment { get; set; } = null!;

    public EquipmentEventType Type { get; set; }

    public required string Description { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? CheckoutId { get; set; }

    public Checkout? Checkout { get; set; }
}
