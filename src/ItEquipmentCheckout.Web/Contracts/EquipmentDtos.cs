using System.ComponentModel.DataAnnotations;
using ItEquipmentCheckout.Core.Entities;

namespace ItEquipmentCheckout.Web.Contracts;

public sealed record EquipmentDto(
    Guid Id,
    string InventoryNumber,
    EquipmentCategory Category,
    string Manufacturer,
    string Model,
    string SerialNumber,
    EquipmentStatus Status,
    int ExpectedReturnDays,
    CheckoutSummaryDto? ActiveCheckout,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateEquipmentRequest
{
    [Required, StringLength(40, MinimumLength = 2)]
    public string InventoryNumber { get; init; } = string.Empty;

    [EnumDataType(typeof(EquipmentCategory))]
    public EquipmentCategory Category { get; init; }

    [Required, StringLength(80, MinimumLength = 1)]
    public string Manufacturer { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Model { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string SerialNumber { get; init; } = string.Empty;

    [Range(1, 365)]
    public int ExpectedReturnDays { get; init; } = 30;
}

public sealed class UpdateEquipmentRequest : CreateEquipmentRequest;

public sealed class EquipmentStatusRequest
{
    [StringLength(300)]
    public string? Reason { get; init; }
}

public sealed record CsvImportResult(
    int Imported,
    int Rejected,
    IReadOnlyList<CsvImportError> Errors);

public sealed record CsvImportError(int Row, string Message);
