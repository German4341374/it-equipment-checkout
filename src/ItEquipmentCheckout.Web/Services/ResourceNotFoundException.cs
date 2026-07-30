namespace ItEquipmentCheckout.Web.Services;

public sealed class ResourceNotFoundException(string resource, Guid id)
    : Exception($"{resource} with ID '{id}' was not found.")
{
    public string Resource { get; } = resource;

    public Guid ResourceId { get; } = id;
}
