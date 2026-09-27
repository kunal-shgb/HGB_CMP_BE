using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingOtps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tracking_otps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    salt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracking_otps", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracking_otps_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tracking_otps_complaint_id_created_at",
                table: "tracking_otps",
                columns: new[] { "complaint_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tracking_otps");
        }
    }
}
