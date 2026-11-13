using AutoPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Persistence;

public static class InventoryMockSeeder
{
    public const string PartNumberPrefix = InventoryMockData.PartNumberPrefix;

    /// <summary>
    /// Ensures at least <paramref name="mockItemCount"/> inventory rows whose part numbers start with <see cref="PartNumberPrefix"/>.
    /// Idempotent: existing MOCK-* rows are counted toward the target.
    /// </summary>
    public static async Task<int> EnsureMockItemsAsync(
        AutoProDbContext db,
        int mockItemCount,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        if (mockItemCount <= 0)
            return 0;

        var existingMockCount = await db.InventoryItems
            .CountAsync(i => i.PartNumber.StartsWith(PartNumberPrefix), ct);

        if (existingMockCount >= mockItemCount)
        {
            logger?.LogInformation(
                "Inventory mock data already present ({Existing} >= {Target})",
                existingMockCount,
                mockItemCount);
            return 0;
        }

        var existingNumbers = await db.InventoryItems
            .AsNoTracking()
            .Where(i => i.PartNumber.StartsWith(PartNumberPrefix))
            .Select(i => i.PartNumber)
            .ToListAsync(ct);
        var existingSet = existingNumbers.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var added = new List<InventoryItem>();

        foreach (var row in InventoryMockData.GenerateRows(mockItemCount, now))
        {
            if (existingSet.Contains(row.PartNumber))
                continue;

            added.Add(new InventoryItem
            {
                Id = row.Id,
                PartNumber = row.PartNumber,
                Name = row.Name,
                Notes = row.Notes,
                QuantityOnHand = row.QuantityOnHand,
                UnitPrice = row.UnitPrice,
                IsActive = row.IsActive,
                CreatedAtUtc = row.CreatedAtUtc,
            });
        }

        if (added.Count == 0)
            return 0;

        db.InventoryItems.AddRange(added);
        await db.SaveChangesAsync(ct);

        logger?.LogInformation("Inserted {Count} mock inventory items (prefix {Prefix})", added.Count, PartNumberPrefix);
        return added.Count;
    }
}
