using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAndApprovalRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approver_department",
                table: "complaint_approvals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.key);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "key", "updated_at", "updated_by", "value" },
                values: new object[] { "approvals.ho_maker_checker_department", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null });

            migrationBuilder.InsertData(
                table: "application_role_mapping",
                columns: new[] { "id", "application_role", "created_at", "iam_role", "is_active", "office_type", "updated_at" },
                values: new object[] { new Guid("4f8a4b00-80d3-2321-b3ab-7c6a82020de6"), "ADMIN", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", true, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("4f8a4b00-80d3-2321-b3ab-7c6a82020de6"));

            migrationBuilder.DropColumn(
                name: "approver_department",
                table: "complaint_approvals");
        }
    }
}
