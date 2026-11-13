using AutoPro.Domain.Entities;
using AutoPro.Domain.Enums;
using AutoPro.Domain.Services;
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
            db.StaffUsers.AddRange(
                new StaffUser
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111001"),
                    UserName = "cr.demo",
                    DisplayName = "Demo Customer Rep",
                    Role = UserRole.CustomerRepresentative,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                    IsActive = true
                },
                new StaffUser
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111002"),
                    UserName = "ao.demo",
                    DisplayName = "Demo Officer",
                    Role = UserRole.AdministrativeOfficer,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                    IsActive = true
                });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo users: cr.demo and ao.demo / ChangeMe123!");
        }
        else if (!await db.StaffUsers.AnyAsync(u => u.UserName == "ao.demo", ct))
        {
            db.StaffUsers.Add(new StaffUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111002"),
                UserName = "ao.demo",
                DisplayName = "Demo Officer",
                Role = UserRole.AdministrativeOfficer,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo AO user: ao.demo / ChangeMe123!");
        }

        var existingPriceTypes = await db.JobTypeServicePrices.Select(p => p.JobType).ToListAsync(ct);
        var missingPriceTypes = Enum.GetValues<JobType>().Except(existingPriceTypes).ToList();
        if (missingPriceTypes.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var jobType in missingPriceTypes)
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
            logger.LogInformation("Seeded default job type service prices.");
        }

        if (!await db.OrganizationSettings.AnyAsync(o => o.Id == OrganizationSettings.SingletonId, ct))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                Id = OrganizationSettings.SingletonId,
                OrganizationName = string.Empty,
                UpdatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded organization settings row.");
        }
    }
}
