using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "scan_status",
                table: "complaint_attachments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "NOT_SCANNED");

            migrationBuilder.AddColumn<string>(
                name: "sha256",
                table: "complaint_attachments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by_name",
                table: "complaint_attachments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scan_status",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "sha256",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "uploaded_by_name",
                table: "complaint_attachments");
        }
    }
}
