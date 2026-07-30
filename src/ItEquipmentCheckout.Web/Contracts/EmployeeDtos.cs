using System.ComponentModel.DataAnnotations;

namespace ItEquipmentCheckout.Web.Contracts;

public sealed record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Email,
    string Department,
    int ActiveCheckoutCount,
    DateTime CreatedAt);

public class CreateEmployeeRequest
{
    [Required, StringLength(30, MinimumLength = 2)]
    public string EmployeeNumber { get; init; } = string.Empty;

    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Department { get; init; } = string.Empty;
}

public sealed class UpdateEmployeeRequest : CreateEmployeeRequest;
