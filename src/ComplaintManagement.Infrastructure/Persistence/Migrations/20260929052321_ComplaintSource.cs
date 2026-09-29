using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComplaintSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "lodged_by_employee_id",
                table: "complaints",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lodged_by_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lodged_by_office_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "complaints",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "WEBSITE");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_source",
                table: "complaints",
                column: "source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_complaints_source",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "lodged_by_employee_id",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "lodged_by_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "lodged_by_office_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "source",
                table: "complaints");
        }
    }
}
