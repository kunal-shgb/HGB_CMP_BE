using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMakerCheckerApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_approval_pending",
                table: "complaint_statuses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_approval",
                table: "complaint_status_transitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "complaint_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    previous_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    maker_remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    requested_by_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    requested_by_office_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approver_level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    approver_office_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    decided_by_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    decided_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_approvals_complaint_statuses_previous_status_code",
                        column: x => x.previous_status_code,
                        principalTable: "complaint_statuses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaint_approvals_complaint_statuses_requested_status_code",
                        column: x => x.requested_status_code,
                        principalTable: "complaint_statuses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaint_approvals_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "application_role_mapping",
                columns: new[] { "id", "application_role", "created_at", "iam_role", "is_active", "office_type", "updated_at" },
                values: new object[,]
                {
                    { new Guid("03a1bc9c-a4b4-3e70-0243-1219fc510e93"), "MAKER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Maker", true, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c8adcc93-d5ba-b281-e87e-ad90154c4925"), "CHECKER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Checker", true, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 1,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 2,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 3,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 4,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 5,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 6,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 7,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 8,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 9,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 10,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 11,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 12,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 13,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 14,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 15,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 16,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 17,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 18,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 19,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 20,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 21,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 22,
                column: "requires_approval",
                value: true);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 23,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 24,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 25,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 26,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 27,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 28,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_status_transitions",
                keyColumn: "id",
                keyValue: 29,
                column: "requires_approval",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "ASSIGNED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "CLOSED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "CUSTOMER_RESPONSE",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "DUPLICATE",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "ESCALATED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "NEW",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "RECEIVED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "REJECTED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "REOPENED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "RESOLVED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "TRANSFERRED",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "UNDER_PROCESS",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.UpdateData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "WITHDRAWN",
                column: "is_approval_pending",
                value: false);

            migrationBuilder.InsertData(
                table: "complaint_statuses",
                columns: new[] { "code", "customer_label", "is_active", "is_approval_pending", "is_assignment", "is_initial", "is_resolution", "is_terminal", "name", "sort_order" },
                values: new object[] { "PENDING_APPROVAL", "Under process", true, true, false, false, false, false, "Pending checker approval", 75 });

            migrationBuilder.CreateIndex(
                name: "ix_complaint_approvals_previous_status_code",
                table: "complaint_approvals",
                column: "previous_status_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_approvals_requested_status_code",
                table: "complaint_approvals",
                column: "requested_status_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_approvals_status_approver_level_approver_office_c~",
                table: "complaint_approvals",
                columns: new[] { "status", "approver_level", "approver_office_code" });

            migrationBuilder.CreateIndex(
                name: "ux_complaint_approvals_one_pending",
                table: "complaint_approvals",
                column: "complaint_id",
                unique: true,
                filter: "status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "complaint_approvals");

            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("03a1bc9c-a4b4-3e70-0243-1219fc510e93"));

            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("c8adcc93-d5ba-b281-e87e-ad90154c4925"));

            migrationBuilder.DeleteData(
                table: "complaint_statuses",
                keyColumn: "code",
                keyValue: "PENDING_APPROVAL");

            migrationBuilder.DropColumn(
                name: "is_approval_pending",
                table: "complaint_statuses");

            migrationBuilder.DropColumn(
                name: "requires_approval",
                table: "complaint_status_transitions");
        }
    }
}
