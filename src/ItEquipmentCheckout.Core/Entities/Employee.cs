namespace ItEquipmentCheckout.Core.Entities;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string EmployeeNumber { get; set; }

    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required string Department { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Checkout> Checkouts { get; } = new List<Checkout>();
}
