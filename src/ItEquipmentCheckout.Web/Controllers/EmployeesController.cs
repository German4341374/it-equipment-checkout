using ItEquipmentCheckout.Core.Entities;
using ItEquipmentCheckout.Web.Contracts;
using ItEquipmentCheckout.Web.Data;
using ItEquipmentCheckout.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(CheckoutDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EmployeeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.Checkouts);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(employee =>
                EF.Functions.Like(employee.EmployeeNumber, pattern) ||
                EF.Functions.Like(employee.FullName, pattern) ||
                EF.Functions.Like(employee.Email, pattern) ||
                EF.Functions.Like(employee.Department, pattern));
        }

        List<Employee> employees = await query
            .OrderBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);
        return Ok(employees.Select(employee => employee.ToDto()).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok((await LoadAsync(id, cancellationToken)).ToDto());
    }

    [HttpPost]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeDto>> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            EmployeeNumber = request.EmployeeNumber,
            FullName = request.FullName,
            Email = request.Email,
            Department = request.Department,
        };
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee.ToDto());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto>> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        Employee employee = await LoadAsync(id, cancellationToken);
        employee.EmployeeNumber = request.EmployeeNumber;
        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.Department = request.Department;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(employee.ToDto());
    }

    private async Task<Employee> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Employees
            .Include(employee => employee.Checkouts)
            .SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Employee", id);
}
