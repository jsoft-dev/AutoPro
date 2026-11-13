using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomServiceChargeForOtherJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomServiceChargeLkr",
                table: "ServiceJobs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomServiceChargeLkr",
                table: "ServiceJobs");
        }
    }
}
