namespace ItEquipmentCheckout.Core.Entities;

public sealed class Checkout
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EquipmentId { get; set; }

    public Equipment Equipment { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public DateTime CheckoutDate { get; set; }

    public DateTime PlannedReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsActive => ActualReturnDate is null;

    public bool IsOverdue(DateTime utcNow) =>
        ActualReturnDate is null && PlannedReturnDate.Date < utcNow.Date;
}
