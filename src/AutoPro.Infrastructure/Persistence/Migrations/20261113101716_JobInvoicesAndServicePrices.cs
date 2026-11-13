using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JobInvoicesAndServicePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalizedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobInvoices_ServiceJobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "ServiceJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTypeServicePrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<int>(type: "integer", nullable: false),
                    ServiceChargeLkr = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTypeServicePrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobInvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: true),
                    JobPurchaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobInvoiceLineItems_JobInvoices_JobInvoiceId",
                        column: x => x.JobInvoiceId,
                        principalTable: "JobInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobInvoiceLineItems_JobPurchaseLines_JobPurchaseLineId",
                        column: x => x.JobPurchaseLineId,
                        principalTable: "JobPurchaseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobInvoiceLineItems_JobInvoiceId",
                table: "JobInvoiceLineItems",
                column: "JobInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInvoiceLineItems_JobPurchaseLineId",
                table: "JobInvoiceLineItems",
                column: "JobPurchaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_JobInvoices_ServiceJobId",
                table: "JobInvoices",
                column: "ServiceJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobTypeServicePrices_JobType",
                table: "JobTypeServicePrices",
                column: "JobType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobInvoiceLineItems");

            migrationBuilder.DropTable(
                name: "JobTypeServicePrices");

            migrationBuilder.DropTable(
                name: "JobInvoices");
        }
    }
}
