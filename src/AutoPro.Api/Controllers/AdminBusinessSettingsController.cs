using AutoPro.Application.Contracts.BusinessSettings;
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
[Route("api/v1/admin/business-settings")]
public class AdminBusinessSettingsController(
    AutoProDbContext db,
    OrganizationLogoStore logoStore) : ControllerBase
{
    private const long MaxLogoBytes = 2 * 1024 * 1024;

    [HttpGet("organization")]
    public async Task<ActionResult<OrganizationSettingsResponse>> GetOrganization(CancellationToken ct)
    {
        var row = await EnsureOrganizationAsync(ct);
        return Ok(ToOrganizationResponse(row));
    }

    [HttpPut("organization")]
    public async Task<ActionResult<OrganizationSettingsResponse>> UpdateOrganization(
        [FromBody] UpdateOrganizationSettingsRequest request,
        CancellationToken ct)
    {
        var name = request.OrganizationName?.Trim() ?? string.Empty;
        if (name.Length == 0)
            return BadRequest(new { message = "Organization name is required." });
        if (name.Length > 200)
            return BadRequest(new { message = "Organization name is too long." });

        var tagline = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim();
        if (tagline?.Length > 200)
            return BadRequest(new { message = "Tagline is too long." });

        var row = await EnsureOrganizationAsync(ct);
        row.OrganizationName = name;
        row.Tagline = tagline;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ToOrganizationResponse(row));
    }

    [HttpPost("organization/logo")]
    [RequestSizeLimit(MaxLogoBytes)]
    public async Task<ActionResult<OrganizationSettingsResponse>> UploadOrganizationLogo(
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Choose an image file to upload." });
        if (file.Length > MaxLogoBytes)
            return BadRequest(new { message = "Logo must be 2 MB or smaller." });

        var contentType = file.ContentType ?? string.Empty;
        if (!IsAllowedLogoContentType(contentType, file.FileName))
            return BadRequest(new { message = "Logo must be PNG, JPEG, or WebP." });

        await using var stream = file.OpenReadStream();
        try
        {
            await logoStore.SaveAsync(stream, contentType, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var row = await EnsureOrganizationAsync(ct);
        return Ok(ToOrganizationResponse(row));
    }

    [HttpDelete("organization/logo")]
    public async Task<ActionResult<OrganizationSettingsResponse>> DeleteOrganizationLogo(CancellationToken ct)
    {
        await logoStore.ClearAsync(ct);

        var row = await EnsureOrganizationAsync(ct);
        return Ok(ToOrganizationResponse(row));
    }

    [HttpGet("job-type-prices")]
    public async Task<ActionResult<IReadOnlyList<JobTypePriceResponse>>> GetJobTypePrices(CancellationToken ct)
    {
        await EnsureDefaultPricesAsync(ct);

        var rows = await db.JobTypeServicePrices.AsNoTracking()
            .OrderBy(p => p.JobType)
            .ToListAsync(ct);

        return Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPut("job-type-prices")]
    public async Task<ActionResult<IReadOnlyList<JobTypePriceResponse>>> UpdateJobTypePrices(
        [FromBody] UpdateJobTypePricesRequest request,
        CancellationToken ct)
    {
        if (request.Prices is null || request.Prices.Count == 0)
            return BadRequest(new { message = "At least one job type price is required." });

        await EnsureDefaultPricesAsync(ct);

        var byType = request.Prices.ToDictionary(p => p.JobType);
        var rows = await db.JobTypeServicePrices.ToListAsync(ct);

        foreach (var row in rows)
        {
            if (!byType.TryGetValue(row.JobType, out var update))
                continue;
            if (update.ServiceChargeLkr < 0)
                return BadRequest(new { message = "Service charge cannot be negative." });
            row.ServiceChargeLkr = update.ServiceChargeLkr;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        var unknown = byType.Keys.Except(rows.Select(r => r.JobType)).ToList();
        if (unknown.Count > 0)
            return BadRequest(new { message = $"Unknown job type(s): {string.Join(", ", unknown)}." });

        await db.SaveChangesAsync(ct);

        var updated = await db.JobTypeServicePrices.AsNoTracking()
            .OrderBy(p => p.JobType)
            .ToListAsync(ct);

        return Ok(updated.Select(ToResponse).ToList());
    }

    private async Task EnsureDefaultPricesAsync(CancellationToken ct)
    {
        var existing = await db.JobTypeServicePrices.Select(p => p.JobType).ToListAsync(ct);
        var missing = Enum.GetValues<JobType>().Except(existing).ToList();
        if (missing.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var jobType in missing)
        {
            db.JobTypeServicePrices.Add(new JobTypeServicePrice
            {
                Id = Guid.NewGuid(),
                JobType = jobType,
                ServiceChargeLkr = JobTypeServicePriceDefaults.ServiceChargeLkr(jobType),
                UpdatedAtUtc = now,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static JobTypePriceResponse ToResponse(JobTypeServicePrice row) =>
        new(
            row.JobType,
            JobTypeDisplay.Label(row.JobType, otherDescription: null),
            row.ServiceChargeLkr,
            row.UpdatedAtUtc);

    private async Task<OrganizationSettings> EnsureOrganizationAsync(CancellationToken ct)
    {
        var existing = await db.OrganizationSettings
            .FirstOrDefaultAsync(o => o.Id == OrganizationSettings.SingletonId, ct);
        if (existing is not null)
            return existing;

        var row = new OrganizationSettings
        {
            Id = OrganizationSettings.SingletonId,
            OrganizationName = string.Empty,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.OrganizationSettings.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    private OrganizationSettingsResponse ToOrganizationResponse(OrganizationSettings row)
    {
        var hasLogo = OrganizationLogoStore.HasLogoData(row);
        string? logoUrl = null;
        if (hasLogo)
        {
            var version = row.UpdatedAtUtc.Ticks;
            logoUrl = $"{Request.Scheme}://{Request.Host}/api/v1/media/organization-logo?v={version}";
        }

        return new OrganizationSettingsResponse(
            row.OrganizationName,
            row.Tagline,
            hasLogo,
            logoUrl,
            row.UpdatedAtUtc);
    }

    private static bool IsAllowedLogoContentType(string contentType, string fileName)
    {
        var type = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (type is "image/png" or "image/jpeg" or "image/webp")
            return true;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp";
    }
}
