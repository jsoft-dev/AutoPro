using AutoPro.Application.Contracts.Inventory;
using AutoPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "CustomerRepresentative")]
[Route("api/v1/inventory")]
public class InventoryController(AutoProDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    [HttpGet("search")]
    public async Task<ActionResult<InventorySearchPageResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = DefaultPageSize,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, MaxPageSize);
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
}
