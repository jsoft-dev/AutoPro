using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public class PurchaseOrderNumberGenerator(AutoProDbContext db)
{
    public async Task<string> NextAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PO-{year}-";
        var last = await db.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PoNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PoNumber)
            .Select(p => p.PoNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last.AsSpan(prefix.Length), out var n))
            seq = n + 1;

        return $"{prefix}{seq:D5}";
    }
}