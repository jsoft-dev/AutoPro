using Microsoft.EntityFrameworkCore.Migrations;

namespace AutoPro.Infrastructure.Persistence;

internal static class InventoryMockMigrationSql
{
    internal static void Apply(MigrationBuilder migrationBuilder, int mockItemCount)
    {
        foreach (var row in InventoryMockData.GenerateRows(mockItemCount))
        {
            var notes = InventoryMockData.SqlLiteral(row.Notes);
            var createdAt = row.CreatedAtUtc.ToString("O");
            var quantity = row.QuantityOnHand.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var unitPrice = row.UnitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var isActive = row.IsActive ? "TRUE" : "FALSE";

            migrationBuilder.Sql(
                $"""
                INSERT INTO "InventoryItems" (
                    "Id", "PartNumber", "Name", "Notes",
                    "QuantityOnHand", "UnitPrice", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
                )
                SELECT
                    '{row.Id}',
                    {InventoryMockData.SqlLiteral(row.PartNumber)},
                    {InventoryMockData.SqlLiteral(row.Name)},
                    {notes},
                    {quantity},
                    {unitPrice},
                    {isActive},
                    TIMESTAMPTZ '{createdAt}',
                    NULL
                WHERE NOT EXISTS (
                    SELECT 1 FROM "InventoryItems" WHERE "PartNumber" = {InventoryMockData.SqlLiteral(row.PartNumber)}
                );
                """);
        }
    }

    internal static void Revert(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(
            $"""
            DELETE FROM "InventoryItems"
            WHERE "PartNumber" LIKE '{InventoryMockData.PartNumberPrefix.Replace("'", "''", StringComparison.Ordinal)}%';
            """);
}
