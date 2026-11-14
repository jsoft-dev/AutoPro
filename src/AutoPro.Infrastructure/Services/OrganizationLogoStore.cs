using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Infrastructure.Services;

public class OrganizationLogoStore(AutoProDbContext db)
{
    public static bool HasLogoData(OrganizationSettings? row) =>
        row?.LogoData is { Length: > 0 };

    public async Task SaveAsync(Stream content, string contentType, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length == 0)
            throw new InvalidOperationException("Logo file is empty.");

        var normalizedType = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (normalizedType is not ("image/png" or "image/jpeg" or "image/webp"))
            throw new InvalidOperationException("Logo must be PNG, JPEG, or WebP.");

        var row = await db.OrganizationSettings
            .FirstOrDefaultAsync(o => o.Id == OrganizationSettings.SingletonId, ct);
        if (row is null)
        {
            row = new OrganizationSettings
            {
                Id = OrganizationSettings.SingletonId,
                OrganizationName = string.Empty,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            db.OrganizationSettings.Add(row);
        }

        row.LogoData = bytes;
        row.LogoContentType = normalizedType;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        var row = await db.OrganizationSettings
            .FirstOrDefaultAsync(o => o.Id == OrganizationSettings.SingletonId, ct);
        if (row is null)
            return;

        row.LogoData = null;
        row.LogoContentType = null;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}