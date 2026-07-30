using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Controllers;

[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController(
    CheckoutDbContext dbContext,
    CheckoutApplicationService checkoutService,
    CsvEquipmentService csvService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EquipmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] EquipmentCategory? category,
        [FromQuery] EquipmentStatus? status,
        [FromQuery] bool overdue = false,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        IQueryable<Equipment> query = dbContext.Equipment
            .AsNoTracking()
            .Include(item => item.Checkouts.Where(checkout => checkout.ActualReturnDate == null))
                .ThenInclude(checkout => checkout.Employee);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.Like(item.InventoryNumber, pattern) ||
                EF.Functions.Like(item.SerialNumber, pattern) ||
                EF.Functions.Like(item.Manufacturer, pattern) ||
                EF.Functions.Like(item.Model, pattern) ||
                item.Checkouts.Any(checkout =>
                    checkout.ActualReturnDate == null &&
                    EF.Functions.Like(checkout.Employee.FullName, pattern)));
        }

        if (category.HasValue)
        {
            query = query.Where(item => item.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (overdue)
        {
            DateTime today = now.Date;
            query = query.Where(item => item.Checkouts.Any(checkout =>
                checkout.ActualReturnDate == null &&
                checkout.PlannedReturnDate < today));
        }

        List<EquipmentDto> result = await query
            .OrderBy(item => item.InventoryNumber)
            .Select(item => item)
            .AsAsyncEnumerable()
            .Select(item => item.ToDto(now))
            .ToListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await GetDtoAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EquipmentDto>> Create(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var item = new Equipment
        {
            InventoryNumber = request.InventoryNumber,
            Category = request.Category,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            ExpectedReturnDays = request.ExpectedReturnDays,
        };
        item.Events.Add(new EquipmentEvent
        {
            EquipmentId = item.Id,
            Type = EquipmentEventType.Created,
            Description = "Equipment created.",
        });
        dbContext.Equipment.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        EquipmentDto response = await GetDtoAsync(item.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentDto>> Update(
        Guid id,
        UpdateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        Equipment item = await dbContext.Equipment
            .Include(equipment => equipment.Events)
            .SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Equipment", id);
        item.InventoryNumber = request.InventoryNumber;
        item.Category = request.Category;
        item.Manufacturer = request.Manufacturer;
        item.Model = request.Model;
        item.SerialNumber = request.SerialNumber;
        item.ExpectedReturnDays = request.ExpectedReturnDays;
        item.Events.Add(new EquipmentEvent
        {
            EquipmentId = item.Id,
            Type = EquipmentEventType.Updated,
            Description = "Equipment details updated.",
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await GetDtoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/repair")]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentDto>> MarkRepair(
        Guid id,
        EquipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        await checkoutService.MarkRepairAsync(id, request.Reason, cancellationToken);
        return Ok(await GetDtoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/available")]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentDto>> MakeAvailable(
        Guid id,
        CancellationToken cancellationToken)
    {
        await checkoutService.MakeAvailableAsync(id, cancellationToken);
        return Ok(await GetDtoAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/retire")]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentDto>> Retire(
        Guid id,
        EquipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        await checkoutService.RetireAsync(id, request.Reason, cancellationToken);
        return Ok(await GetDtoAsync(id, cancellationToken));
    }

    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        byte[] contents = await csvService.ExportAsync(cancellationToken);
        return File(contents, "text/csv; charset=utf-8", $"equipment-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("import")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [ProducesResponseType<CsvImportResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CsvImportResult>> Import(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > 2 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(file), "CSV file must be between 1 byte and 2 MiB.");
            return ValidationProblem(ModelState);
        }

        await using Stream stream = file.OpenReadStream();
        CsvImportResult result = await csvService.ImportAsync(stream, cancellationToken);
        return Ok(result);
    }

    private async Task<EquipmentDto> GetDtoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Equipment item = await dbContext.Equipment
            .AsNoTracking()
            .Include(equipment => equipment.Checkouts.Where(checkout => checkout.ActualReturnDate == null))
                .ThenInclude(checkout => checkout.Employee)
            .SingleOrDefaultAsync(equipment => equipment.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Equipment", id);
        return item.ToDto(DateTime.UtcNow);
    }
}
