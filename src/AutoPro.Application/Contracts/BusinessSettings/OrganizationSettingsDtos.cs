namespace AutoPro.Application.Contracts.BusinessSettings;

public record OrganizationSettingsResponse(
    string OrganizationName,
    string? Tagline,
    bool HasLogo,
    string? LogoUrl,
    DateTime UpdatedAtUtc);

public record UpdateOrganizationSettingsRequest(
    string OrganizationName,
    string? Tagline);
