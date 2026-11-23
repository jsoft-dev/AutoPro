using System.Security.Claims;
using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.PurchaseOrders;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/purchase-orders")]
public class AdminPurchaseOrdersController(
    AutoProDbContext db,
    PurchaseOrderNumberGenerator poNumbers) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("options")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderOptionResponse>>> Options(
        [FromQuery] Guid? supplierId,
        [FromQuery] bool openOnly = true,
        CancellationToken ct = default)
    {
        var query = db.PurchaseOrders.AsNoTracking().Include(p => p.Supplier).AsQueryable();
        if (supplierId is Guid sid)
            query = query.Where(p => p.SupplierId == sid);
        if (openOnly)
        {
            query = query.Where(p =>
                p.Status == PurchaseOrderStatus.Approved ||
                p.Status == PurchaseOrderStatus.PartiallyReceived);
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PurchaseOrderOptionResponse(
                p.Id,
                p.PoNumber,
                p.SupplierId,
                p.Supplier.Name,
                p.Status))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.PurchaseOrders.AsNoTracking().Include(p => p.Supplier).AsQueryable();

        if (status is not null)
            query = query.Where(p => p.Status == status);
        if (supplierId is Guid sid)
            query = query.Where(p => p.SupplierId == sid);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.PoNumber, $"%{term}%") ||
                EF.Functions.ILike(p.Supplier.Name, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseOrderAdminListItemResponse(
                p.Id,
                p.PoNumber,
                p.SupplierId,
                p.Supplier.Name,
                p.Status,
                p.Lines.Count,
                p.CreatedAtUtc,
                p.ApprovedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<PurchaseOrderAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var po = await LoadDetailQuery().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po is null) return NotFound(new { message = "Purchase order not found." });
        return Ok(ToDetail(po));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderAdminDetailResponse>> Create(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken ct)
    {
        var validation = await ValidateWriteAsync(request.SupplierId, request.Lines, ct);
        if (validation.Error is not null)
            return BadRequest(new { message = validation.Error });

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = await poNumbers.NextAsync(ct),
            SupplierId = request.SupplierId,
            Status = PurchaseOrderStatus.Draft,
            Notes = TrimOrNull(request.Notes),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId
        };

        foreach (var line in validation.Lines!)
            po.Lines.Add(line);

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);

        var created = await LoadDetailQuery().FirstAsync(p => p.Id == po.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = po.Id }, ToDetail(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken ct)
    {
        var po = await db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po is null) return NotFound(new { message = "Purchase order not found." });
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { message = "Only draft purchase orders can be edited." });

        var validation = await ValidateWriteAsync(request.SupplierId, request.Lines, ct);
        if (validation.Error is not null)
            return BadRequest(new { message = validation.Error });

        po.SupplierId = request.SupplierId;
        po.Notes = TrimOrNull(request.Notes);
        po.UpdatedAtUtc = DateTime.UtcNow;

        db.PurchaseOrderLines.RemoveRange(po.Lines);
        po.Lines.Clear();
        foreach (var line in validation.Lines!)
            po.Lines.Add(line);

        await db.SaveChangesAsync(ct);

        var updated = await LoadDetailQuery().FirstAsync(p => p.Id == id, ct);
        return Ok(ToDetail(updated));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PurchaseOrderAdminDetailResponse>> Approve(Guid id, CancellationToken ct)
    {
        var po = await db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po is null) return NotFound(new { message = "Purchase order not found." });
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { message = "Only draft purchase orders can be approved." });
        if (po.Lines.Count == 0)
            return BadRequest(new { message = "Add at least one line before approving." });

        po.Status = PurchaseOrderStatus.Approved;
        po.ApprovedAtUtc = DateTime.UtcNow;
        po.ApprovedByUserId = CurrentUserId;
        po.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var approved = await LoadDetailQuery().FirstAsync(p => p.Id == id, ct);
        return Ok(ToDetail(approved));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseOrderAdminDetailResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var po = await db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (po is null) return NotFound(new { message = "Purchase order not found." });
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { message = "Only draft purchase orders can be cancelled." });

        po.Status = PurchaseOrderStatus.Cancelled;
        po.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cancelled = await LoadDetailQuery().FirstAsync(p => p.Id == id, ct);
        return Ok(ToDetail(cancelled));
    }

    private IQueryable<PurchaseOrder> LoadDetailQuery() =>
        db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .ThenInclude(l => l.InventoryItem);

    private static PurchaseOrderAdminDetailResponse ToDetail(PurchaseOrder po) =>
        new(
            po.Id,
            po.PoNumber,
            po.SupplierId,
            po.Supplier.Name,
            po.Status,
            po.Notes,
            po.CreatedAtUtc,
            po.UpdatedAtUtc,
            po.ApprovedAtUtc,
            po.Lines
                .OrderBy(l => l.InventoryItem.PartNumber)
                .Select(l => new PurchaseOrderLineResponse(
                    l.Id,
                    l.InventoryItemId,
                    l.InventoryItem.PartNumber,
                    l.InventoryItem.Name,
                    l.QuantityOrdered,
                    l.QuantityReceived,
                    PurchaseOrderReceivingRules.Remaining(l),
                    l.UnitCost,
                    l.Notes))
                .ToList());

    private async Task<(string? Error, List<PurchaseOrderLine>? Lines)> ValidateWriteAsync(
        Guid supplierId,
        IReadOnlyList<PurchaseOrderLineWriteRequest> lines,
        CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, ct);
        if (supplier is null)
            return ("Supplier not found or inactive.", null);

        if (lines is null || lines.Count == 0)
            return ("Add at least one line.", null);

        var itemIds = lines.Select(l => l.InventoryItemId).Distinct().ToList();
        if (itemIds.Count != lines.Count)
            return ("Each line must reference a different part.", null);

        var items = await db.InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id) && i.IsActive)
            .Select(i => i.Id)
            .ToListAsync(ct);
        if (items.Count != itemIds.Count)
            return ("One or more parts were not found or are inactive.", null);

        var entities = new List<PurchaseOrderLine>();
        foreach (var line in lines)
        {
            if (line.QuantityOrdered <= 0)
                return ("Ordered quantity must be greater than zero.", null);
            if (line.UnitCost is < 0)
                return ("Unit cost cannot be negative.", null);

            entities.Add(new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                InventoryItemId = line.InventoryItemId,
                QuantityOrdered = line.QuantityOrdered,
                QuantityReceived = 0,
                UnitCost = line.UnitCost,
                Notes = TrimOrNull(line.Notes)
            });
        }

        return (null, entities);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}