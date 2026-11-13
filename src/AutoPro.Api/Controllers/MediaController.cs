using AutoPro.Domain.Entities;
using AutoPro.Infrastructure.Persistence;
using AutoPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPro.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public class MediaController(OrganizationLogoStore logoStore, AutoProDbContext db) : ControllerBase
{
    private const string SingletonContentTypeFallback = "image/png";

    [HttpGet("organization-logo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrganizationLogo(CancellationToken ct)
    {
        var path = logoStore.ResolveExistingPath();
        if (path is null)
            return NotFound();

        var row = await db.OrganizationSettings.FindAsync([OrganizationSettings.SingletonId], ct);
        var contentType = row?.LogoContentType ?? SingletonContentTypeFallback;

        return PhysicalFile(path, contentType, enableRangeProcessing: false);
    }
}
