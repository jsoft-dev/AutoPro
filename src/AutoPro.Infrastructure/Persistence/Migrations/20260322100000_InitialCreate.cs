using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Customers", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "StaffUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Role = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_StaffUsers", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Vehicles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                QrToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PlateNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Vin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                Make = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Year = table.Column<int>(type: "integer", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vehicles", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vehicles_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ServiceJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                OdometerReading = table.Column<int>(type: "integer", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServiceJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ServiceJobs_StaffUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "StaffUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ServiceJobs_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "JobPurchaseLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                Source = table.Column<int>(type: "integer", nullable: false),
                InventoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobPurchaseLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobPurchaseLines_ServiceJobs_ServiceJobId",
                    column: x => x.ServiceJobId,
                    principalTable: "ServiceJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "JobStatusHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                FromStatus = table.Column<int>(type: "integer", nullable: false),
                ToStatus = table.Column<int>(type: "integer", nullable: false),
                ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobStatusHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_JobStatusHistories_ServiceJobs_ServiceJobId",
                    column: x => x.ServiceJobId,
                    principalTable: "ServiceJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Customers_Phone", table: "Customers", column: "Phone");
        migrationBuilder.CreateIndex(name: "IX_StaffUsers_UserName", table: "StaffUsers", column: "UserName", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Vehicles_CustomerId", table: "Vehicles", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_Vehicles_PlateNumber", table: "Vehicles", column: "PlateNumber");
        migrationBuilder.CreateIndex(name: "IX_Vehicles_QrToken", table: "Vehicles", column: "QrToken", unique: true);
        migrationBuilder.CreateIndex(name: "IX_ServiceJobs_CreatedAtUtc", table: "ServiceJobs", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_ServiceJobs_CreatedByUserId", table: "ServiceJobs", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_ServiceJobs_JobNumber", table: "ServiceJobs", column: "JobNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_ServiceJobs_Status", table: "ServiceJobs", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_ServiceJobs_VehicleId", table: "ServiceJobs", column: "VehicleId");
        migrationBuilder.CreateIndex(name: "IX_JobPurchaseLines_ServiceJobId", table: "JobPurchaseLines", column: "ServiceJobId");
        migrationBuilder.CreateIndex(name: "IX_JobStatusHistories_ServiceJobId", table: "JobStatusHistories", column: "ServiceJobId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "JobPurchaseLines");
        migrationBuilder.DropTable(name: "JobStatusHistories");
        migrationBuilder.DropTable(name: "ServiceJobs");
        migrationBuilder.DropTable(name: "Vehicles");
        migrationBuilder.DropTable(name: "StaffUsers");
        migrationBuilder.DropTable(name: "Customers");
    }
}
