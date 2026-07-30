using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Core.Services;

namespace ItEquipmentCheckout.UnitTests;

public sealed class EquipmentCheckoutPolicyTests
{
    private static readonly Guid EmployeeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateTime CheckoutDate = new(2026, 7, 1, 14, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void CheckOutCreatesActiveCheckoutAndEvent()
    {
        Equipment equipment = AvailableEquipment();

        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(10),
            "Developer laptop");

        Assert.Equal(EquipmentStatus.CheckedOut, equipment.Status);
        Assert.Equal(CheckoutDate.Date, checkout.CheckoutDate);
        Assert.Equal(CheckoutDate.AddDays(10).Date, checkout.PlannedReturnDate);
        Assert.Null(checkout.ActualReturnDate);
        Assert.Single(equipment.Checkouts);
        Assert.Contains(equipment.Events, item => item.Type == EquipmentEventType.CheckedOut);
    }

    [Fact]
    public void CheckOutUsesExpectedReturnDaysWhenDateMissing()
    {
        Equipment equipment = AvailableEquipment();
        equipment.ExpectedReturnDays = 21;

        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            plannedReturnDate: null);

        Assert.Equal(CheckoutDate.Date.AddDays(21), checkout.PlannedReturnDate);
    }

    [Theory]
    [InlineData(EquipmentStatus.Repair)]
    [InlineData(EquipmentStatus.Retired)]
    [InlineData(EquipmentStatus.CheckedOut)]
    public void CheckOutRejectsUnavailableStatuses(EquipmentStatus status)
    {
        Equipment equipment = AvailableEquipment();
        equipment.Status = status;

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.CheckOut(
                equipment,
                EmployeeId,
                CheckoutDate,
                CheckoutDate.AddDays(5)));

        Assert.Equal("equipment_not_available", exception.Code);
    }

    [Fact]
    public void CheckOutRejectsSecondActiveCheckout()
    {
        Equipment equipment = AvailableEquipment();
        EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(5));
        equipment.Status = EquipmentStatus.Available;

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.CheckOut(
                equipment,
                Guid.NewGuid(),
                CheckoutDate,
                CheckoutDate.AddDays(7)));

        Assert.Equal("equipment_already_checked_out", exception.Code);
    }

    [Fact]
    public void CheckOutRejectsPlannedReturnBeforeCheckout()
    {
        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.CheckOut(
                AvailableEquipment(),
                EmployeeId,
                CheckoutDate,
                CheckoutDate.AddDays(-1)));

        Assert.Equal("invalid_planned_return_date", exception.Code);
    }

    [Fact]
    public void ReturnCompletesCheckoutAndMakesEquipmentAvailable()
    {
        Equipment equipment = AvailableEquipment();
        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(7));

        EquipmentCheckoutPolicy.Return(equipment, checkout, CheckoutDate.AddDays(3));

        Assert.Equal(CheckoutDate.Date.AddDays(3), checkout.ActualReturnDate);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.Contains(equipment.Events, item => item.Type == EquipmentEventType.Returned);
    }

    [Fact]
    public void ReturnRejectsDateBeforeCheckout()
    {
        Equipment equipment = AvailableEquipment();
        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(7));

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.Return(equipment, checkout, CheckoutDate.AddDays(-1)));

        Assert.Equal("invalid_actual_return_date", exception.Code);
    }

    [Fact]
    public void ReturnRejectsCompletedCheckout()
    {
        Equipment equipment = AvailableEquipment();
        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(7));
        EquipmentCheckoutPolicy.Return(equipment, checkout, CheckoutDate.AddDays(1));

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.Return(equipment, checkout, CheckoutDate.AddDays(2)));

        Assert.Equal("checkout_not_active", exception.Code);
    }

    [Fact]
    public void MarkRepairRejectsCheckedOutEquipment()
    {
        Equipment equipment = AvailableEquipment();
        EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(7));

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.MarkRepair(equipment, "Screen failure"));

        Assert.Equal("active_checkout_exists", exception.Code);
    }

    [Fact]
    public void RepairLifecycleReturnsEquipmentToAvailable()
    {
        Equipment equipment = AvailableEquipment();

        EquipmentCheckoutPolicy.MarkRepair(equipment, "Battery replacement");
        Assert.Equal(EquipmentStatus.Repair, equipment.Status);
        EquipmentCheckoutPolicy.MakeAvailable(equipment);

        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.Contains(equipment.Events, item => item.Type == EquipmentEventType.ReturnedFromRepair);
    }

    [Fact]
    public void RetireRejectsActiveCheckout()
    {
        Equipment equipment = AvailableEquipment();
        EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(7));

        BusinessRuleException exception = Assert.Throws<BusinessRuleException>(() =>
            EquipmentCheckoutPolicy.Retire(equipment, "Lifecycle complete"));

        Assert.Equal("active_checkout_exists", exception.Code);
    }

    [Fact]
    public void CheckoutDetectsOverdueOnlyWhileActive()
    {
        Equipment equipment = AvailableEquipment();
        Checkout checkout = EquipmentCheckoutPolicy.CheckOut(
            equipment,
            EmployeeId,
            CheckoutDate,
            CheckoutDate.AddDays(2));

        Assert.True(checkout.IsOverdue(CheckoutDate.AddDays(3)));
        EquipmentCheckoutPolicy.Return(equipment, checkout, CheckoutDate.AddDays(3));
        Assert.False(checkout.IsOverdue(CheckoutDate.AddDays(4)));
    }

    private static Equipment AvailableEquipment() =>
        new()
        {
            InventoryNumber = "LT-TEST-001",
            Category = EquipmentCategory.Laptop,
            Manufacturer = "Example",
            Model = "Portable",
            SerialNumber = "SERIAL-TEST-001",
            Status = EquipmentStatus.Available,
            ExpectedReturnDays = 30,
        };
}
