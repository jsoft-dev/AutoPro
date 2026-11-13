using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Services;

public class OrganizationLogoStore(IWebHostEnvironment env, ILogger<OrganizationLogoStore> logger)
{
    private const string FolderName = "organization";
    private const string FileBaseName = "logo";

    private string DirectoryPath =>
        Path.Combine(env.ContentRootPath, "AppData", FolderName);

    public bool HasLogo()
    {
        var path = ResolveExistingPath();
        return path is not null;
    }

    public string? ResolveExistingPath()
    {
        if (!Directory.Exists(DirectoryPath))
            return null;

        return Directory
            .EnumerateFiles(DirectoryPath, $"{FileBaseName}.*")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public async Task SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken ct)
    {
        var ext = GuessExtension(contentType, originalFileName);
        if (ext is null)
            throw new InvalidOperationException("Logo must be PNG, JPEG, or WebP.");

        Directory.CreateDirectory(DirectoryPath);

        foreach (var existing in Directory.EnumerateFiles(DirectoryPath, $"{FileBaseName}.*"))
        {
            try
            {
                File.Delete(existing);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete old organization logo {Path}", existing);
            }
        }

        var targetPath = Path.Combine(DirectoryPath, $"{FileBaseName}{ext}");
        await using var fs = File.Create(targetPath);
        await content.CopyToAsync(fs, ct);
    }

    public void Delete()
    {
        if (!Directory.Exists(DirectoryPath))
            return;

        foreach (var file in Directory.EnumerateFiles(DirectoryPath, $"{FileBaseName}.*"))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not delete organization logo {Path}", file);
            }
        }
    }

    private static string? GuessExtension(string contentType, string originalFileName)
    {
        var type = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return type switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => Path.GetExtension(originalFileName).ToLowerInvariant() switch
            {
                ".png" => ".png",
                ".jpg" or ".jpeg" => ".jpg",
                ".webp" => ".webp",
                _ => null,
            },
        };
    }
}
