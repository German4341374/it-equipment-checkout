using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Core.Services;
using ItEquipmentCheckout.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Services;

public sealed class CheckoutApplicationService(CheckoutDbContext dbContext)
{
    public async Task<Checkout> CheckOutAsync(
        Guid equipmentId,
        Guid employeeId,
        DateTime checkoutDate,
        DateTime? plannedReturnDate,
        string? notes,
        CancellationToken cancellationToken)
    {
        Equipment equipment = await dbContext.Equipment
            .Include(item => item.Checkouts)
            .Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken)
            ?? throw new ResourceNotFoundException("Equipment", equipmentId);
        bool employeeExists = await dbContext.Employees
            .AnyAsync(employee => employee.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            throw new ResourceNotFoundException("Employee", employeeId);
        }

        HashSet<Guid> existingEventIds = equipment.Events.Select(item => item.Id).ToHashSet();
        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            employeeId,
            checkoutDate,
            plannedReturnDate,
            notes);
        dbContext.Checkouts.Add(checkout);
        AddNewEvents(equipment, existingEventIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadCheckoutAsync(checkout.Id, cancellationToken);
    }

    public async Task<Checkout> ReturnAsync(
        Guid checkoutId,
        DateTime actualReturnDate,
        CancellationToken cancellationToken)
    {
        Checkout checkout = await dbContext.Checkouts
            .Include(item => item.Equipment)
                .ThenInclude(item => item.Checkouts)
            .Include(item => item.Equipment)
                .ThenInclude(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == checkoutId, cancellationToken)
            ?? throw new ResourceNotFoundException("Checkout", checkoutId);

        HashSet<Guid> existingEventIds = checkout.Equipment.Events
            .Select(item => item.Id)
            .ToHashSet();
        EquipmentCheckoutPolicy.Return(checkout.Equipment, checkout, actualReturnDate);
        AddNewEvents(checkout.Equipment, existingEventIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadCheckoutAsync(checkoutId, cancellationToken);
    }

    public async Task<Equipment> MarkRepairAsync(
        Guid equipmentId,
        string? reason,
        CancellationToken cancellationToken)
    {
        Equipment equipment = await LoadEquipmentForStatusAsync(equipmentId, cancellationToken);
        HashSet<Guid> existingEventIds = equipment.Events.Select(item => item.Id).ToHashSet();
        EquipmentCheckoutPolicy.MarkRepair(equipment, reason);
        AddNewEvents(equipment, existingEventIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return equipment;
    }

    public async Task<Equipment> MakeAvailableAsync(
        Guid equipmentId,
        CancellationToken cancellationToken)
    {
        Equipment equipment = await LoadEquipmentForStatusAsync(equipmentId, cancellationToken);
        HashSet<Guid> existingEventIds = equipment.Events.Select(item => item.Id).ToHashSet();
        EquipmentCheckoutPolicy.MakeAvailable(equipment);
        AddNewEvents(equipment, existingEventIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return equipment;
    }

    public async Task<Equipment> RetireAsync(
        Guid equipmentId,
        string? reason,
        CancellationToken cancellationToken)
    {
        Equipment equipment = await LoadEquipmentForStatusAsync(equipmentId, cancellationToken);
        HashSet<Guid> existingEventIds = equipment.Events.Select(item => item.Id).ToHashSet();
        EquipmentCheckoutPolicy.Retire(equipment, reason);
        AddNewEvents(equipment, existingEventIds);
        await dbContext.SaveChangesAsync(cancellationToken);
        return equipment;
    }

    private async Task<Equipment> LoadEquipmentForStatusAsync(
        Guid equipmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Equipment
            .Include(item => item.Checkouts)
                .ThenInclude(checkout => checkout.Employee)
            .Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == equipmentId, cancellationToken)
            ?? throw new ResourceNotFoundException("Equipment", equipmentId);

    private async Task<Checkout> LoadCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken) =>
        await dbContext.Checkouts
            .AsNoTracking()
            .Include(item => item.Equipment)
            .Include(item => item.Employee)
            .SingleAsync(item => item.Id == checkoutId, cancellationToken);

    private void AddNewEvents(Equipment equipment, HashSet<Guid> existingEventIds)
    {
        dbContext.EquipmentEvents.AddRange(
            equipment.Events.Where(item => !existingEventIds.Contains(item.Id)));
    }
}
