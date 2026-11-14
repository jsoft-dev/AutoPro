using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

if (!string.Equals(configuration["RESET_HOSTED_DATABASE_CONFIRM"], "yes", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine(
        "Destructive reset blocked. Set RESET_HOSTED_DATABASE_CONFIRM=yes to wipe all hosted data.");
    return 1;
}

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "Set ConnectionStrings__DefaultConnection to your Neon (Render API) database URL.");
    return 1;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole());
var logger = loggerFactory.CreateLogger("ResetHostedDatabase");

if (connectionString.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
{
    logger.LogWarning(
        "Connection uses Neon pooler; switching to direct host for DROP SCHEMA / migrations.");
    connectionString = connectionString.Replace("-pooler", "", StringComparison.OrdinalIgnoreCase);
}

var options = new DbContextOptionsBuilder<AutoProDbContext>()
    .UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(AutoProDbContext).Assembly.GetName().Name))
    .Options;

await using var db = new AutoProDbContext(options);

logger.LogWarning("Dropping public schema (all jobs, customers, vehicles, inventory, invoices)…");
await db.Database.ExecuteSqlRawAsync(
    """
    DROP SCHEMA IF EXISTS public CASCADE;
    CREATE SCHEMA public;
    GRANT ALL ON SCHEMA public TO public;
    """);

logger.LogInformation("Applying migrations and seed (demo users, prices, mock inventory)…");
await DataSeeder.SeedAsync(db, logger);

var users = await db.StaffUsers.CountAsync();
var jobs = await db.ServiceJobs.CountAsync();
var vehicles = await db.Vehicles.CountAsync();
var inventory = await db.InventoryItems.CountAsync();

Console.WriteLine(
    $"Hosted database reset complete. StaffUsers={users}, Vehicles={vehicles}, Jobs={jobs}, InventoryItems={inventory}.");
Console.WriteLine("Demo logins: cr.demo and ao.demo / ChangeMe123!");
return 0;
