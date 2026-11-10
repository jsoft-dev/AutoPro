using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public class JobNumberGenerator(AutoProDbContext db)
{
    public async Task<string> NextAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"AP-{year}-";
        var last = await db.ServiceJobs
            .AsNoTracking()
            .Where(j => j.JobNumber.StartsWith(prefix))
            .OrderByDescending(j => j.JobNumber)
            .Select(j => j.JobNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last.AsSpan(prefix.Length), out var n))
            seq = n + 1;

        return $"{prefix}{seq:D5}";
    }
}
