using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPro.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController(AutoProDbContext db) : ControllerBase
{
    private const string SingletonContentTypeFallback = "image/png";

    [HttpGet("organization-logo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrganizationLogo(CancellationToken ct)
    {
        var row = await db.OrganizationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == OrganizationSettings.SingletonId, ct);

        if (!OrganizationLogoStore.HasLogoData(row))
            return NotFound();

        var contentType = row!.LogoContentType ?? SingletonContentTypeFallback;
        return File(row.LogoData!, contentType);
    }
}