namespace AutoPro.Domain.Entities;

/// <summary>Singleton row: the service station (client) identity shown on invoices.</summary>
public class OrganizationSettings
{
    public static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Guid Id { get; set; }

    public string OrganizationName { get; set; } = string.Empty;

    public string? Tagline { get; set; }

    public string? LogoContentType { get; set; }

    /// <summary>PNG/JPEG/WebP bytes (stored in Postgres so hosted API survives ephemeral disk).</summary>
    public byte[]? LogoData { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
