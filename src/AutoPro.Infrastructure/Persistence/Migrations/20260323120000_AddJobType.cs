using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPro.Infrastructure.Persistence.Migrations;

public partial class AddJobType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "JobType",
            table: "ServiceJobs",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "JobTypeOtherDescription",
            table: "ServiceJobs",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "JobType", table: "ServiceJobs");
        migrationBuilder.DropColumn(name: "JobTypeOtherDescription", table: "ServiceJobs");
    }
}
