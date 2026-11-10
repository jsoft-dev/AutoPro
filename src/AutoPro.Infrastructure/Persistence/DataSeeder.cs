using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoPro.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AutoProDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.StaffUsers.AnyAsync(ct))
        {
            db.StaffUsers.Add(new StaffUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111001"),
                UserName = "cr.demo",
                DisplayName = "Demo Customer Rep",
                Role = UserRole.CustomerRepresentative,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo CR user: cr.demo / ChangeMe123!");
        }
    }
}
