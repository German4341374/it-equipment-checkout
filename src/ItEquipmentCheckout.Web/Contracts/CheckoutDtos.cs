using System.ComponentModel.DataAnnotations;

namespace ItEquipmentCheckout.Web.Contracts;

public sealed record CheckoutDto(
    Guid Id,
    Guid EquipmentId,
    string InventoryNumber,
    string EquipmentName,
    Guid EmployeeId,
    string EmployeeName,
    DateTime CheckoutDate,
    DateTime PlannedReturnDate,
    DateTime? ActualReturnDate,
    bool IsOverdue,
    string Notes);

public sealed record CheckoutSummaryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime CheckoutDate,
    DateTime PlannedReturnDate,
    bool IsOverdue);

public sealed class CreateCheckoutRequest : IValidatableObject
{
    [Required]
    public Guid EquipmentId { get; init; }

    [Required]
    public Guid EmployeeId { get; init; }

    public DateTime? CheckoutDate { get; init; }

    public DateTime? PlannedReturnDate { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckoutDate.HasValue &&
            PlannedReturnDate.HasValue &&
            PlannedReturnDate.Value.Date < CheckoutDate.Value.Date)
        {
            yield return new ValidationResult(
                "Planned return date cannot be before checkout date.",
                [nameof(PlannedReturnDate)]);
        }
    }
}

public sealed class ReturnCheckoutRequest
{
    public DateTime? ActualReturnDate { get; init; }
}
