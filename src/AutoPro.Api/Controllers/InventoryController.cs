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
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<InventorySearchItemResponse>>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = db.InventoryItems.AsNoTracking()
            .Where(i => i.IsActive && i.QuantityOnHand > 0);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, $"%{term}%") ||
                EF.Functions.ILike(i.PartNumber, $"%{term}%"));
        }

        var items = await query
            .OrderBy(i => i.Name)
            .Take(limit)
            .Select(i => new InventorySearchItemResponse(
                i.Id,
                i.PartNumber,
                i.Name,
                i.QuantityOnHand,
                i.UnitPrice))
            .ToListAsync(ct);

        return Ok(items);
    }
}
