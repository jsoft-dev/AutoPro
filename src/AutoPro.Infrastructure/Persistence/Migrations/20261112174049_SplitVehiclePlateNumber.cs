using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitVehiclePlateNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlatePrefix",
                table: "Vehicles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlateSuffix",
                table: "Vehicles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Vehicles"
                SET
                  "PlatePrefix" = UPPER(SPLIT_PART("PlateNumber", '-', 1)),
                  "PlateSuffix" = SPLIT_PART("PlateNumber", '-', 2)
                WHERE POSITION('-' IN "PlateNumber") > 0;

                UPDATE "Vehicles"
                SET
                  "PlatePrefix" = UPPER(TRIM("PlateNumber")),
                  "PlateSuffix" = '0'
                WHERE POSITION('-' IN "PlateNumber") = 0 AND TRIM("PlateNumber") <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PlatePrefix_PlateSuffix",
                table: "Vehicles",
                columns: new[] { "PlatePrefix", "PlateSuffix" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_PlatePrefix_PlateSuffix",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PlatePrefix",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PlateSuffix",
                table: "Vehicles");
        }
    }
}
