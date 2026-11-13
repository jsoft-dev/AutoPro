using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_Vehicles_VehicleId",
                table: "ServiceJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "VehicleId",
                table: "ServiceJobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "StoreCustomerName",
                table: "ServiceJobs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_Vehicles_VehicleId",
                table: "ServiceJobs",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceJobs_Vehicles_VehicleId",
                table: "ServiceJobs");

            migrationBuilder.DropColumn(
                name: "StoreCustomerName",
                table: "ServiceJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "VehicleId",
                table: "ServiceJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceJobs_Vehicles_VehicleId",
                table: "ServiceJobs",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
