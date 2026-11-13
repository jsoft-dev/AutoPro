using AutoPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInventoryMockData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            InventoryMockMigrationSql.Apply(migrationBuilder, InventoryMockData.DefaultMockCount);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            InventoryMockMigrationSql.Revert(migrationBuilder);
    }
}
