using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.Inventory;
using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/inventory")]
public class AdminInventoryController(AutoProDbContext db) : ControllerBase
{
    private const int SearchDefaultPageSize = 20;
    private const int SearchMaxPageSize = 50;

    /// <summary>
    /// Parts with stock, for picking on a job purchase line (same rules as the mobile app).
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<InventorySearchPageResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = SearchDefaultPageSize,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, SearchMaxPageSize);
        offset = Math.Max(0, offset);

        var query = db.InventoryItems.AsNoTracking()
            .Where(i => i.IsActive && i.QuantityOnHand > 0);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, $"%{term}%") ||
                EF.Functions.ILike(i.PartNumber, $"%{term}%"));
        }

        var rows = await query
            .OrderBy(i => i.PartNumber)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Id)
            .Skip(offset)
            .Take(limit + 1)
            .Select(i => new InventorySearchItemResponse(
                i.Id,
                i.PartNumber,
                i.Name,
                i.QuantityOnHand,
                i.UnitPrice))
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        return Ok(new InventorySearchPageResponse(page, hasMore));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.InventoryItems.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, $"%{term}%") ||
                EF.Functions.ILike(i.PartNumber, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var mapped = items.Select(ToListItem).ToList();
        return Ok(new PagedResult<InventoryAdminListItemResponse>(mapped, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound(new { message = "Part not found." });
        return Ok(ToDetail(item));
    }

    [HttpPost]
    public async Task<ActionResult<InventoryAdminDetailResponse>> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken ct)
    {
        var partNumber = request.PartNumber.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(partNumber))
            return BadRequest(new { message = "Part number is required." });
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { message = "Name is required." });
        if (request.QuantityOnHand < 0)
            return BadRequest(new { message = "Quantity on hand cannot be negative." });
        if (request.UnitPrice < 0)
            return BadRequest(new { message = "Unit price cannot be negative." });

        var exists = await db.InventoryItems.AnyAsync(i => i.PartNumber == partNumber, ct);
        if (exists)
            return Conflict(new { message = "A part with this part number already exists." });

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            PartNumber = partNumber,
            Name = name,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            QuantityOnHand = request.QuantityOnHand,
            UnitPrice = request.UnitPrice,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToDetail(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken ct)
    {
        var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound(new { message = "Part not found." });

        var partNumber = request.PartNumber.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(partNumber))
            return BadRequest(new { message = "Part number is required." });
        if (string.IsNullOrEmpty(name))
            return BadRequest(new { message = "Name is required." });
        if (request.QuantityOnHand < 0)
            return BadRequest(new { message = "Quantity on hand cannot be negative." });
        if (request.UnitPrice < 0)
            return BadRequest(new { message = "Unit price cannot be negative." });

        var duplicate = await db.InventoryItems.AnyAsync(
            i => i.PartNumber == partNumber && i.Id != id, ct);
        if (duplicate)
            return Conflict(new { message = "A part with this part number already exists." });

        item.PartNumber = partNumber;
        item.Name = name;
        item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.QuantityOnHand = request.QuantityOnHand;
        item.UnitPrice = request.UnitPrice;
        item.IsActive = request.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToDetail(item));
    }

    private static InventoryAdminListItemResponse ToListItem(InventoryItem item) =>
        new(item.Id, item.PartNumber, item.Name, item.QuantityOnHand, item.UnitPrice, item.IsActive, item.CreatedAtUtc);

    private static InventoryAdminDetailResponse ToDetail(InventoryItem item) =>
        new(
            item.Id,
            item.PartNumber,
            item.Name,
            item.Notes,
            item.QuantityOnHand,
            item.UnitPrice,
            item.IsActive,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
}
