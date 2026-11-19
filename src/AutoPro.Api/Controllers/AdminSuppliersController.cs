using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Suppliers;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/suppliers")]
public class AdminSuppliersController(AutoProDbContext db) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<IReadOnlyList<SupplierOptionResponse>>> Options(CancellationToken ct)
    {
        var items = await db.Suppliers.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Select(s => new SupplierOptionResponse(s.Id, s.Name))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Suppliers.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.Email != null && EF.Functions.ILike(s.Email, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Name)
            .ThenByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SupplierAdminListItemResponse(
                s.Id,
                s.Name,
                s.Phone,
                s.Email,
                s.InventoryItems.Count,
                s.IsActive,
                s.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<SupplierAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return NotFound(new { message = "Supplier not found." });
        return Ok(ToDetail(supplier));
    }

    [HttpPost]
    public async Task<ActionResult<SupplierAdminDetailResponse>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken ct)
    {
        var error = ValidateWrite(request.Name);
        if (error is not null) return BadRequest(new { message = error });

        var name = request.Name.Trim();
        var duplicate = await db.Suppliers.AnyAsync(
            s => EF.Functions.ILike(s.Name, name), ct);
        if (duplicate)
            return Conflict(new { message = "A supplier with this name already exists." });

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactPerson = TrimOrNull(request.ContactPerson),
            Phone = TrimOrNull(request.Phone),
            Email = TrimOrNull(request.Email),
            Address = TrimOrNull(request.Address),
            Notes = TrimOrNull(request.Notes),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, ToDetail(supplier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken ct)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return NotFound(new { message = "Supplier not found." });

        var error = ValidateWrite(request.Name);
        if (error is not null) return BadRequest(new { message = error });

        var name = request.Name.Trim();
        var duplicate = await db.Suppliers.AnyAsync(
            s => s.Id != id && EF.Functions.ILike(s.Name, name), ct);
        if (duplicate)
            return Conflict(new { message = "A supplier with this name already exists." });

        supplier.Name = name;
        supplier.ContactPerson = TrimOrNull(request.ContactPerson);
        supplier.Phone = TrimOrNull(request.Phone);
        supplier.Email = TrimOrNull(request.Email);
        supplier.Address = TrimOrNull(request.Address);
        supplier.Notes = TrimOrNull(request.Notes);
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToDetail(supplier));
    }

    private static string? ValidateWrite(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required.";
        if (name.Trim().Length > 200) return "Name is too long.";
        return null;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierAdminDetailResponse ToDetail(Supplier supplier) =>
        new(
            supplier.Id,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.Notes,
            supplier.IsActive,
            supplier.CreatedAtUtc,
            supplier.UpdatedAtUtc);
}