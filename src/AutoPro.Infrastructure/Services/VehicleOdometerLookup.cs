using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public static class VehicleOdometerLookup
{
    /// <summary>
    /// Latest non-null odometer on a prior job for this vehicle (by job opened date).
    /// </summary>
    public static Task<int?> GetLastRecordedAsync(
        AutoProDbContext db,
        Guid vehicleId,
        Guid? excludeJobId = null,
        CancellationToken cancellationToken = default)
    {
        var q = db.ServiceJobs.AsNoTracking()
            .Where(j => j.VehicleId == vehicleId && j.OdometerReading != null);

        if (excludeJobId is Guid exclude)
            q = q.Where(j => j.Id != exclude);

        return q
            .OrderByDescending(j => j.CreatedAtUtc)
            .ThenByDescending(j => j.Id)
            .Select(j => j.OdometerReading)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
