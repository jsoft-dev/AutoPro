using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Customers;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/customers")]
public class AdminCustomersController(AutoProDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, $"%{term}%") ||
                c.Phone.Contains(term) ||
                (c.Email != null && EF.Functions.ILike(c.Email, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerAdminListItemResponse(
                c.Id,
                c.FullName,
                c.Phone,
                c.Email,
                c.Vehicles.Count,
                c.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<CustomerAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await CustomerDetailQuery(db.Customers.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync(ct);

        if (customer is null) return NotFound(new { message = "Customer not found." });

        return Ok(customer);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null) return NotFound(new { message = "Customer not found." });

        var fullName = request.FullName.Trim();
        var phone = request.Phone.Trim();
        if (string.IsNullOrEmpty(fullName))
            return BadRequest(new { message = "Name is required." });
        if (string.IsNullOrEmpty(phone))
            return BadRequest(new { message = "Phone is required." });

        customer.FullName = fullName;
        customer.Phone = phone;
        customer.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        await db.SaveChangesAsync(ct);

        var detail = await CustomerDetailQuery(db.Customers.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync(ct);

        return Ok(detail!);
    }

    private static IQueryable<CustomerAdminDetailResponse> CustomerDetailQuery(IQueryable<Domain.Entities.Customer> query) =>
        query.Select(c => new CustomerAdminDetailResponse(
            c.Id,
            c.FullName,
            c.Phone,
            c.Email,
            c.CreatedAtUtc,
            c.Vehicles
                .OrderByDescending(v => v.CreatedAtUtc)
                .Select(v => new CustomerAdminVehicleSummary(
                    v.Id,
                    v.PlateNumber,
                    v.Make,
                    v.Model,
                    v.Year,
                    v.CreatedAtUtc))
                .ToList()));
}
