using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ItEquipmentCheckout.IntegrationTests;

public sealed class ApiIntegrationTests(CheckoutWebFactory factory)
    : IClassFixture<CheckoutWebFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient client = factory.CreateClient();

    public void Dispose() => client.Dispose();

    [Fact]
    public async Task HealthEndpointReportsHealthy()
    {
        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"healthy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EquipmentCanBeCreatedSearchedAndLoaded()
    {
        EquipmentDto created = await CreateEquipmentAsync("SEARCH");

        EquipmentDto? loaded = await client.GetFromJsonAsync<EquipmentDto>(
            $"/api/equipment/{created.Id}",
            JsonOptions);
        IReadOnlyList<EquipmentDto>? search = await client.GetFromJsonAsync<List<EquipmentDto>>(
            $"/api/equipment?search={created.InventoryNumber}",
            JsonOptions);

        Assert.NotNull(loaded);
        Assert.Equal(created.InventoryNumber, loaded.InventoryNumber);
        Assert.Contains(search!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task DuplicateInventoryNumberReturnsProblemDetails()
    {
        EquipmentDto created = await CreateEquipmentAsync("DUPLICATE");
        var request = new CreateEquipmentRequest
        {
            InventoryNumber = created.InventoryNumber,
            Category = EquipmentCategory.Monitor,
            Manufacturer = "Example",
            Model = "Other",
            SerialNumber = $"SERIAL-{Guid.NewGuid():N}",
            ExpectedReturnDays = 14,
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/equipment", request);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Database constraint conflict", problem?.Title);
    }

    [Fact]
    public async Task CheckoutPreventsDoubleAssignmentAndAllowsReturn()
    {
        EquipmentDto equipment = await CreateEquipmentAsync("FLOW");
        EmployeeDto firstEmployee = await CreateEmployeeAsync("FLOW-A");
        EmployeeDto secondEmployee = await CreateEmployeeAsync("FLOW-B");
        var request = new CreateCheckoutRequest
        {
            EquipmentId = equipment.Id,
            EmployeeId = firstEmployee.Id,
            CheckoutDate = DateTime.UtcNow.Date,
            PlannedReturnDate = DateTime.UtcNow.Date.AddDays(7),
        };

        using HttpResponseMessage first = await client.PostAsJsonAsync("/api/checkouts", request);
        CheckoutDto? checkout = await first.Content.ReadFromJsonAsync<CheckoutDto>(JsonOptions);
        using HttpResponseMessage duplicate = await client.PostAsJsonAsync(
            "/api/checkouts",
            new CreateCheckoutRequest
            {
                EquipmentId = equipment.Id,
                EmployeeId = secondEmployee.Id,
                PlannedReturnDate = DateTime.UtcNow.Date.AddDays(10),
            });
        using HttpResponseMessage returned = await client.PostAsJsonAsync(
            $"/api/checkouts/{checkout!.Id}/return",
            new ReturnCheckoutRequest { ActualReturnDate = DateTime.UtcNow.Date });
        using HttpResponseMessage second = await client.PostAsJsonAsync(
            "/api/checkouts",
            new CreateCheckoutRequest
            {
                EquipmentId = equipment.Id,
                EmployeeId = secondEmployee.Id,
                PlannedReturnDate = DateTime.UtcNow.Date.AddDays(10),
            });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task RepairEquipmentCannotBeCheckedOut()
    {
        EquipmentDto equipment = await CreateEquipmentAsync("REPAIR");
        EmployeeDto employee = await CreateEmployeeAsync("REPAIR");
        using HttpResponseMessage repair = await client.PostAsJsonAsync(
            $"/api/equipment/{equipment.Id}/repair",
            new EquipmentStatusRequest { Reason = "Integration test" });

        using HttpResponseMessage checkout = await client.PostAsJsonAsync(
            "/api/checkouts",
            new CreateCheckoutRequest
            {
                EquipmentId = equipment.Id,
                EmployeeId = employee.Id,
                PlannedReturnDate = DateTime.UtcNow.AddDays(5),
            });

        Assert.Equal(HttpStatusCode.OK, repair.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, checkout.StatusCode);
    }

    [Fact]
    public async Task ReturnBeforeCheckoutDateIsRejected()
    {
        EquipmentDto equipment = await CreateEquipmentAsync("DATE");
        EmployeeDto employee = await CreateEmployeeAsync("DATE");
        DateTime checkoutDate = DateTime.UtcNow.Date.AddDays(2);
        using HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/checkouts",
            new CreateCheckoutRequest
            {
                EquipmentId = equipment.Id,
                EmployeeId = employee.Id,
                CheckoutDate = checkoutDate,
                PlannedReturnDate = checkoutDate.AddDays(7),
            });
        CheckoutDto? checkout = await created.Content.ReadFromJsonAsync<CheckoutDto>(JsonOptions);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/checkouts/{checkout!.Id}/return",
            new ReturnCheckoutRequest { ActualReturnDate = checkoutDate.AddDays(-1) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ValidationProducesProblemDetails()
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/equipment",
            new CreateEquipmentRequest
            {
                InventoryNumber = string.Empty,
                Manufacturer = string.Empty,
                Model = string.Empty,
                SerialNumber = string.Empty,
                ExpectedReturnDays = 0,
            });
        ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(problem!.Errors);
    }

    [Fact]
    public async Task CsvImportAndExportRoundTripData()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string csv = string.Join(
            Environment.NewLine,
            "inventoryNumber,category,manufacturer,model,serialNumber,expectedReturnDays",
            $"CSV-{suffix},Monitor,Example,Panel,SERIAL-CSV-{suffix},45");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes(csv)),
            "file",
            "equipment.csv");

        using HttpResponseMessage imported = await client.PostAsync("/api/equipment/import", multipart);
        CsvImportResult? result = await imported.Content.ReadFromJsonAsync<CsvImportResult>();
        using HttpResponseMessage exported = await client.GetAsync("/api/equipment/export");
        string exportBody = await exported.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        Assert.Equal(1, result?.Imported);
        Assert.Equal(HttpStatusCode.OK, exported.StatusCode);
        Assert.Contains($"CSV-{suffix}", exportBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashboardReflectsCreatedEquipment()
    {
        await CreateEquipmentAsync("DASHBOARD");

        DashboardDto? dashboard = await client.GetFromJsonAsync<DashboardDto>(
            "/api/dashboard",
            JsonOptions);

        Assert.NotNull(dashboard);
        Assert.True(dashboard.TotalEquipment >= 1);
        Assert.True(dashboard.Available >= 1);
    }

    private async Task<EquipmentDto> CreateEquipmentAsync(string prefix)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateEquipmentRequest
        {
            InventoryNumber = $"{prefix}-{suffix}",
            Category = EquipmentCategory.Laptop,
            Manufacturer = "Example",
            Model = "Test Device",
            SerialNumber = $"SERIAL-{prefix}-{suffix}",
            ExpectedReturnDays = 30,
        };
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/equipment", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EquipmentDto>(JsonOptions))!;
    }

    private async Task<EmployeeDto> CreateEmployeeAsync(string prefix)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateEmployeeRequest
        {
            EmployeeNumber = $"{prefix}-{suffix}",
            FullName = $"Test Employee {suffix}",
            Email = $"{prefix.ToLowerInvariant()}-{suffix}@example.test",
            Department = "Testing",
        };
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>(JsonOptions))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
