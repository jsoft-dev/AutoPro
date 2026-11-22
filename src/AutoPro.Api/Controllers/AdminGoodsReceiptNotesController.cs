using System.Security.Claims;
using AutoPro.Application.Contracts.Common;
using AutoPro.Application.Contracts.GoodsReceipts;
using AutoPro.Application.Inventory;
using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AutoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "AdministrativeOfficer")]
[Route("api/v1/admin/goods-receipts")]
public class AdminGoodsReceiptNotesController(
    AutoProDbContext db,
    GoodsReceiptNumberGenerator grnNumbers,
    IGoodsReceiptPostingService postingService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<GoodsReceiptAdminListItemResponse>>> List(
        [FromQuery] string? q,
        [FromQuery] GoodsReceiptNoteStatus? status,
        [FromQuery] Guid? supplierId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.GoodsReceiptNotes.AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .AsQueryable();

        if (status is not null)
            query = query.Where(g => g.Status == status);
        if (supplierId is Guid sid)
            query = query.Where(g => g.SupplierId == sid);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(g =>
                EF.Functions.ILike(g.GrnNumber, $"%{term}%") ||
                EF.Functions.ILike(g.Supplier.Name, $"%{term}%") ||
                (g.SupplierReference != null && EF.Functions.ILike(g.SupplierReference, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(g => g.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GoodsReceiptAdminListItemResponse(
                g.Id,
                g.GrnNumber,
                g.SupplierId,
                g.Supplier.Name,
                g.PurchaseOrderId,
                g.PurchaseOrder != null ? g.PurchaseOrder.PoNumber : null,
                g.Status,
                g.ReceivedAtUtc,
                g.Lines.Count,
                g.CreatedAtUtc,
                g.PostedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<GoodsReceiptAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoodsReceiptAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var grn = await LoadDetailQuery().FirstOrDefaultAsync(g => g.Id == id, ct);
        if (grn is null) return NotFound(new { message = "Goods receipt not found." });
        return Ok(ToDetail(grn));
    }

    [HttpPost]
    public async Task<ActionResult<GoodsReceiptAdminDetailResponse>> Create(
        [FromBody] CreateGoodsReceiptRequest request,
        CancellationToken ct)
    {
        var validation = await ValidateWriteAsync(
            request.SupplierId,
            request.PurchaseOrderId,
            request.Lines,
            ct);
        if (validation.Error is not null)
            return BadRequest(new { message = validation.Error });

        var grn = new GoodsReceiptNote
        {
            Id = Guid.NewGuid(),
            GrnNumber = await grnNumbers.NextAsync(ct),
            SupplierId = request.SupplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            Status = GoodsReceiptNoteStatus.Draft,
            ReceivedAtUtc = ToUtc(request.ReceivedAtUtc),
            SupplierReference = TrimOrNull(request.SupplierReference),
            Notes = TrimOrNull(request.Notes),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId
        };

        foreach (var line in validation.Lines!)
        {
            line.GoodsReceiptNoteId = grn.Id;
            grn.Lines.Add(line);
        }

        db.GoodsReceiptNotes.Add(grn);

        var saveError = await TrySaveAsync(ct);
        if (saveError is not null)
            return BadRequest(new { message = saveError });

        var created = await LoadDetailQuery().FirstAsync(g => g.Id == grn.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = grn.Id }, ToDetail(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GoodsReceiptAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateGoodsReceiptRequest request,
        CancellationToken ct)
    {
        var grn = await db.GoodsReceiptNotes.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (grn is null) return NotFound(new { message = "Goods receipt not found." });
        if (grn.Status != GoodsReceiptNoteStatus.Draft)
            return BadRequest(new { message = "Only draft goods receipts can be edited." });

        var validation = await ValidateWriteAsync(
            request.SupplierId,
            request.PurchaseOrderId,
            request.Lines,
            ct);
        if (validation.Error is not null)
            return BadRequest(new { message = validation.Error });

        grn.SupplierId = request.SupplierId;
        grn.PurchaseOrderId = request.PurchaseOrderId;
        grn.ReceivedAtUtc = ToUtc(request.ReceivedAtUtc);
        grn.SupplierReference = TrimOrNull(request.SupplierReference);
        grn.Notes = TrimOrNull(request.Notes);
        grn.UpdatedAtUtc = DateTime.UtcNow;

        await db.GoodsReceiptNoteLines
            .Where(l => l.GoodsReceiptNoteId == id)
            .ExecuteDeleteAsync(ct);

        foreach (var line in validation.Lines!)
        {
            line.GoodsReceiptNoteId = grn.Id;
            db.GoodsReceiptNoteLines.Add(line);
        }

        var saveError = await TrySaveAsync(ct);
        if (saveError is not null)
            return BadRequest(new { message = saveError });

        var updated = await LoadDetailQuery().FirstAsync(g => g.Id == id, ct);
        return Ok(ToDetail(updated));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<GoodsReceiptAdminDetailResponse>> Post(
        Guid id,
        [FromBody] PostGoodsReceiptRequest request,
        CancellationToken ct)
    {
        var grn = await db.GoodsReceiptNotes
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
        if (grn is null) return NotFound(new { message = "Goods receipt not found." });

        var error = await postingService.PostAsync(grn, request.UpdateSellingPriceFromUnitCost, CurrentUserId, ct);
        if (error is not null)
            return BadRequest(new { message = error });

        var posted = await LoadDetailQuery().FirstAsync(g => g.Id == id, ct);
        return Ok(ToDetail(posted));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<GoodsReceiptAdminDetailResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var grn = await db.GoodsReceiptNotes.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (grn is null) return NotFound(new { message = "Goods receipt not found." });
        if (grn.Status != GoodsReceiptNoteStatus.Draft)
            return BadRequest(new { message = "Only draft goods receipts can be cancelled." });

        grn.Status = GoodsReceiptNoteStatus.Cancelled;
        grn.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cancelled = await LoadDetailQuery().FirstAsync(g => g.Id == id, ct);
        return Ok(ToDetail(cancelled));
    }

    private IQueryable<GoodsReceiptNote> LoadDetailQuery() =>
        db.GoodsReceiptNotes.AsNoTracking()
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Lines)
            .ThenInclude(l => l.InventoryItem);

    private static GoodsReceiptAdminDetailResponse ToDetail(GoodsReceiptNote grn) =>
        new(
            grn.Id,
            grn.GrnNumber,
            grn.SupplierId,
            grn.Supplier.Name,
            grn.PurchaseOrderId,
            grn.PurchaseOrder?.PoNumber,
            grn.Status,
            grn.ReceivedAtUtc,
            grn.SupplierReference,
            grn.Notes,
            grn.CreatedAtUtc,
            grn.UpdatedAtUtc,
            grn.PostedAtUtc,
            grn.Lines
                .OrderBy(l => l.InventoryItem.PartNumber)
                .Select(l => new GoodsReceiptLineResponse(
                    l.Id,
                    l.InventoryItemId,
                    l.InventoryItem.PartNumber,
                    l.InventoryItem.Name,
                    l.PurchaseOrderLineId,
                    l.QuantityReceived,
                    l.UnitCost,
                    l.Notes))
                .ToList());

    private async Task<(string? Error, List<GoodsReceiptNoteLine>? Lines)> ValidateWriteAsync(
        Guid supplierId,
        Guid? purchaseOrderId,
        IReadOnlyList<GoodsReceiptLineWriteRequest> lines,
        CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, ct);
        if (supplier is null)
            return ("Supplier not found or inactive.", null);

        PurchaseOrder? po = null;
        if (purchaseOrderId is Guid poId)
        {
            po = await db.PurchaseOrders.AsNoTracking()
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == poId, ct);
            if (po is null)
                return ("Purchase order not found.", null);
            if (po.SupplierId != supplierId)
                return ("Supplier must match the linked purchase order.", null);

            var poError = PurchaseOrderReceivingRules.ValidateCanReceiveAgainst(po);
            if (poError is not null)
                return (poError, null);
        }

        if (lines is null || lines.Count == 0)
            return ("Add at least one line.", null);

        var itemIds = lines.Select(l => l.InventoryItemId).ToList();
        var items = await db.InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var entities = new List<GoodsReceiptNoteLine>();
        foreach (var line in lines)
        {
            if (!items.ContainsKey(line.InventoryItemId))
                return ("One or more parts were not found.", null);
            if (line.QuantityReceived <= 0)
                return ("Received quantity must be greater than zero.", null);
            if (line.UnitCost is < 0)
                return ("Unit cost cannot be negative.", null);

            if (po is not null)
            {
                if (line.PurchaseOrderLineId is not Guid poLineId)
                    return ("Each line must reference a purchase order line when a PO is linked.", null);

                var poLine = po.Lines.FirstOrDefault(l => l.Id == poLineId);
                if (poLine is null)
                    return ("Invalid purchase order line reference.", null);
                if (poLine.InventoryItemId != line.InventoryItemId)
                    return ("Line part does not match the purchase order line.", null);

            }

            entities.Add(new GoodsReceiptNoteLine
            {
                Id = Guid.NewGuid(),
                InventoryItemId = line.InventoryItemId,
                PurchaseOrderLineId = line.PurchaseOrderLineId,
                QuantityReceived = line.QuantityReceived,
                UnitCost = line.UnitCost,
                Notes = TrimOrNull(line.Notes)
            });
        }

        return (null, entities);
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private async Task<string?> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "This draft was changed elsewhere. Refresh the page and try again.";
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pg)
            {
                return pg.SqlState switch
                {
                    PostgresErrorCodes.UndefinedTable =>
                        "Database is missing GRN tables. Restart the API so migrations apply, or run EF migrations.",
                    PostgresErrorCodes.ForeignKeyViolation =>
                        "A linked supplier, item, or PO line was not found. Refresh and try again.",
                    _ => $"Could not save: {pg.MessageText}"
                };
            }

            return "Could not save goods receipt. Check server logs for details.";
        }
    }
}