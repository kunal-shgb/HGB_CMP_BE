using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "complaint_escalations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_level = table.Column<int>(type: "integer", nullable: false),
                    to_level = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    escalated_by = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    escalated_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    escalated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_escalations", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_escalations_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "app_settings",
                columns: new[] { "key", "updated_at", "updated_by", "value" },
                values: new object[,]
                {
                    { "escalation.enabled", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "true" },
                    { "escalation.to_ho_after_overdue_days", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "7" },
                    { "escalation.to_ro_after_overdue_days", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "0" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_escalation_level",
                table: "complaints",
                column: "escalation_level");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_escalations_complaint_id_escalated_at",
                table: "complaint_escalations",
                columns: new[] { "complaint_id", "escalated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "complaint_escalations");

            migrationBuilder.DropIndex(
                name: "ix_complaints_escalation_level",
                table: "complaints");

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "key",
                keyValue: "escalation.enabled");

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "key",
                keyValue: "escalation.to_ho_after_overdue_days");

            migrationBuilder.DeleteData(
                table: "app_settings",
                keyColumn: "key",
                keyValue: "escalation.to_ro_after_overdue_days");
        }
    }
}
