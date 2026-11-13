using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "Set ConnectionStrings__DefaultConnection (or pass nothing and use user-secrets / env).");
    return 1;
}

var countText = configuration["SEED_INVENTORY_MOCK_COUNT"] ?? "250";
if (!int.TryParse(countText, out var mockCount) || mockCount <= 0)
{
    Console.Error.WriteLine("SEED_INVENTORY_MOCK_COUNT must be a positive integer.");
    return 1;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole());
var logger = loggerFactory.CreateLogger("SeedInventoryMock");

var options = new DbContextOptionsBuilder<AutoProDbContext>()
    .UseNpgsql(
        connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(AutoProDbContext).Assembly.GetName().Name))
    .Options;

await using var db = new AutoProDbContext(options);
await db.Database.MigrateAsync();

var inserted = await InventoryMockSeeder.EnsureMockItemsAsync(db, mockCount, logger);
var total = await db.InventoryItems.CountAsync();
var mockTotal = await db.InventoryItems.CountAsync(i => i.PartNumber.StartsWith(InventoryMockSeeder.PartNumberPrefix));

Console.WriteLine($"Inserted {inserted} rows. Inventory total: {total} (mock: {mockTotal}).");
return 0;
