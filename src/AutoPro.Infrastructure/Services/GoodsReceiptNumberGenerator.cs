using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public class GoodsReceiptNumberGenerator(AutoProDbContext db)
{
    public async Task<string> NextAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"GRN-{year}-";
        var last = await db.GoodsReceiptNotes
            .AsNoTracking()
            .Where(g => g.GrnNumber.StartsWith(prefix))
            .OrderByDescending(g => g.GrnNumber)
            .Select(g => g.GrnNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last.AsSpan(prefix.Length), out var n))
            seq = n + 1;

        return $"{prefix}{seq:D5}";
    }
}