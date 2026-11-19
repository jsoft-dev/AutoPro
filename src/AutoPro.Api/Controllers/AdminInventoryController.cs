using AutoPro.Api.Services;
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
    private const int ExportMaxRows = 10_000;

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
        [FromQuery] Guid? supplierId,
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

        if (supplierId is not null)
            query = query.Where(i => i.SupplierId == supplierId);

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
            .Select(i => new InventoryAdminListItemResponse(
                i.Id,
                i.PartNumber,
                i.Name,
                i.QuantityOnHand,
                i.UnitPrice,
                i.SupplierId,
                i.Supplier != null ? i.Supplier.Name : null,
                i.IsActive,
                i.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<InventoryAdminListItemResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("export")]
    public async Task<ActionResult> ExportToExcel(
        [FromQuery] string? q,
        [FromQuery] Guid? supplierId,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = db.InventoryItems.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(i => i.IsActive);

        if (supplierId is not null)
            query = query.Where(i => i.SupplierId == supplierId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i =>
                EF.Functions.ILike(i.Name, $"%{term}%") ||
                EF.Functions.ILike(i.PartNumber, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(ct);
        if (totalCount > ExportMaxRows)
        {
            return BadRequest(new
            {
                message = $"Too many parts to export ({totalCount}). Narrow your search or filter first (max {ExportMaxRows})."
            });
        }

        var rows = await query
            .OrderBy(i => i.PartNumber)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.Id)
            .Select(i => new InventoryExcelExportRow(
                i.PartNumber,
                i.Name,
                i.QuantityOnHand,
                i.UnitPrice,
                i.Notes,
                i.Supplier != null ? i.Supplier.Name : null,
                i.IsActive))
            .ToListAsync(ct);

        var bytes = InventoryExcelExporter.Build(rows);
        var fileName = $"inventory-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryAdminDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound(new { message = "Part not found." });
        return Ok(await ToDetailAsync(item, ct));
    }

    [HttpPost]
    public async Task<ActionResult<InventoryAdminDetailResponse>> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken ct)
    {
        var validation = await ValidateWriteAsync(request, ct);
        if (validation.Error is not null)
            return validation.Error;

        var partNumber = request.PartNumber.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

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
            SupplierId = request.SupplierId,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, await ToDetailAsync(item, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryAdminDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken ct)
    {
        var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound(new { message = "Part not found." });

        var validation = await ValidateWriteAsync(request, ct);
        if (validation.Error is not null)
            return validation.Error;

        var partNumber = request.PartNumber.Trim().ToUpperInvariant();
        var name = request.Name.Trim();

        var duplicate = await db.InventoryItems.AnyAsync(
            i => i.PartNumber == partNumber && i.Id != id, ct);
        if (duplicate)
            return Conflict(new { message = "A part with this part number already exists." });

        item.PartNumber = partNumber;
        item.Name = name;
        item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.QuantityOnHand = request.QuantityOnHand;
        item.UnitPrice = request.UnitPrice;
        item.SupplierId = request.SupplierId;
        item.IsActive = request.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(await ToDetailAsync(item, ct));
    }

    [HttpGet("import/template")]
    public ActionResult DownloadImportTemplate()
    {
        var bytes = InventoryExcelTemplateBuilder.Build();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            InventoryExcelTemplateBuilder.FileName);
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<InventoryImportResultResponse>> ImportFromExcel(
        IFormFile file,
        [FromQuery] bool createMissingSuppliers = true,
        CancellationToken ct = default)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Choose an Excel file to import." });

        var fileName = file.FileName;
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only .xlsx or .xlsm workbooks are supported." });
        }

        IReadOnlyList<ParsedInventoryImportRow> parsedRows;
        try
        {
            await using var stream = file.OpenReadStream();
            parsedRows = InventoryExcelImportParser.Parse(stream);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (parsedRows.Count == 0)
            return BadRequest(new { message = "No data rows found below the header row." });

        var errors = new List<InventoryImportRowError>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        var supplierCache = await db.Suppliers
            .ToDictionaryAsync(s => s.Name.ToUpperInvariant(), s => s, ct);

        foreach (var row in parsedRows)
        {
            if (string.IsNullOrWhiteSpace(row.PartNumber))
            {
                errors.Add(new InventoryImportRowError(row.RowNumber, "Part number is required."));
                skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                errors.Add(new InventoryImportRowError(row.RowNumber, "Name is required."));
                skipped++;
                continue;
            }

            if (row.QuantityOnHand < 0)
            {
                errors.Add(new InventoryImportRowError(row.RowNumber, "Quantity cannot be negative."));
                skipped++;
                continue;
            }

            if (row.UnitPrice < 0)
            {
                errors.Add(new InventoryImportRowError(row.RowNumber, "Unit price cannot be negative."));
                skipped++;
                continue;
            }

            Guid? supplierId = null;
            if (!string.IsNullOrWhiteSpace(row.SupplierName))
            {
                var supplierKey = row.SupplierName.Trim().ToUpperInvariant();
                if (!supplierCache.TryGetValue(supplierKey, out var supplier))
                {
                    if (!createMissingSuppliers)
                    {
                        errors.Add(new InventoryImportRowError(
                            row.RowNumber,
                            $"Supplier \"{row.SupplierName}\" not found."));
                        skipped++;
                        continue;
                    }

                    supplier = new Supplier
                    {
                        Id = Guid.NewGuid(),
                        Name = row.SupplierName.Trim(),
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    db.Suppliers.Add(supplier);
                    supplierCache[supplierKey] = supplier;
                }

                supplierId = supplier.Id;
            }

            var partNumber = row.PartNumber.Trim().ToUpperInvariant();
            var existing = await db.InventoryItems.FirstOrDefaultAsync(i => i.PartNumber == partNumber, ct);
            if (existing is null)
            {
                db.InventoryItems.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    PartNumber = partNumber,
                    Name = row.Name.Trim(),
                    Notes = row.Notes,
                    QuantityOnHand = row.QuantityOnHand,
                    UnitPrice = row.UnitPrice,
                    SupplierId = supplierId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
                created++;
            }
            else
            {
                existing.Name = row.Name.Trim();
                existing.Notes = row.Notes;
                existing.QuantityOnHand = row.QuantityOnHand;
                existing.UnitPrice = row.UnitPrice;
                if (supplierId is not null)
                    existing.SupplierId = supplierId;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                updated++;
            }
        }

        await db.SaveChangesAsync(ct);
        return Ok(new InventoryImportResultResponse(created, updated, skipped, errors));
    }

    private async Task<(ActionResult? Error, bool Ok)> ValidateWriteAsync(
        CreateInventoryItemRequest request,
        CancellationToken ct)
    {
        var partNumber = request.PartNumber.Trim();
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(partNumber))
            return (BadRequest(new { message = "Part number is required." }), false);
        if (string.IsNullOrEmpty(name))
            return (BadRequest(new { message = "Name is required." }), false);
        if (request.QuantityOnHand < 0)
            return (BadRequest(new { message = "Quantity on hand cannot be negative." }), false);
        if (request.UnitPrice < 0)
            return (BadRequest(new { message = "Unit price cannot be negative." }), false);

        if (request.SupplierId is not null)
        {
            var supplierExists = await db.Suppliers.AnyAsync(s => s.Id == request.SupplierId, ct);
            if (!supplierExists)
                return (BadRequest(new { message = "Supplier not found." }), false);
        }

        return (null, true);
    }

    private Task<(ActionResult? Error, bool Ok)> ValidateWriteAsync(
        UpdateInventoryItemRequest request,
        CancellationToken ct) =>
        ValidateWriteAsync(
            new CreateInventoryItemRequest(
                request.PartNumber,
                request.Name,
                request.Notes,
                request.QuantityOnHand,
                request.UnitPrice,
                request.SupplierId,
                request.IsActive),
            ct);

    private async Task<InventoryAdminDetailResponse> ToDetailAsync(InventoryItem item, CancellationToken ct)
    {
        string? supplierName = null;
        if (item.SupplierId is not null)
        {
            supplierName = item.Supplier?.Name
                ?? await db.Suppliers.AsNoTracking()
                    .Where(s => s.Id == item.SupplierId)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync(ct);
        }

        return new InventoryAdminDetailResponse(
            item.Id,
            item.PartNumber,
            item.Name,
            item.Notes,
            item.QuantityOnHand,
            item.UnitPrice,
            item.SupplierId,
            supplierName,
            item.IsActive,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }
}