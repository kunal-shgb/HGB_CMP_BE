using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_role_mapping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    iam_role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    application_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_role_mapping", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    record_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "complaint_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "complaint_number_sequences",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_number_sequences", x => x.year);
                });

            migrationBuilder.CreateTable(
                name: "complaint_priorities",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_priorities", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "complaint_statuses",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false),
                    is_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    is_resolution = table.Column<bool>(type: "boolean", nullable: false),
                    is_assignment = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_statuses", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "complaint_status_transitions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    from_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    to_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    requires_remark = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_status_transitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_status_transitions_complaint_statuses_from_status~",
                        column: x => x.from_status_code,
                        principalTable: "complaint_statuses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaint_status_transitions_complaint_statuses_to_status_c~",
                        column: x => x.to_status_code,
                        principalTable: "complaint_statuses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaint_subcategories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tat_days = table.Column<int>(type: "integer", nullable: true),
                    default_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_priority_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_subcategories", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_subcategories_complaint_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "complaint_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaint_subcategories_complaint_priorities_default_priori~",
                        column: x => x.default_priority_code,
                        principalTable: "complaint_priorities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaint_subcategories_departments_default_department_id",
                        column: x => x.default_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_regions_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    account_number = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    preferred_channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: true),
                    transaction_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    assigned_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    assigned_employee_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    assigned_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    escalation_level = table.Column<int>(type: "integer", nullable: false),
                    sla_due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaints_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_complaint_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "complaint_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_complaint_priorities_priority_code",
                        column: x => x.priority_code,
                        principalTable: "complaint_priorities",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_complaint_statuses_status_code",
                        column: x => x.status_code,
                        principalTable: "complaint_statuses",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_complaint_subcategories_sub_category_id",
                        column: x => x.sub_category_id,
                        principalTable: "complaint_subcategories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_complaints_departments_assigned_department_id",
                        column: x => x.assigned_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaint_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    assigned_to_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_to_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    assigned_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assigned_by_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_assignments_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_complaint_assignments_departments_assigned_department_id",
                        column: x => x.assigned_department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaint_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_attachments_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "complaint_remarks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    remark = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_by_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_remarks", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_remarks_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "complaint_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    complaint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    new_status_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    remarks = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    changed_by_employee_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaint_status_history_complaints_complaint_id",
                        column: x => x.complaint_id,
                        principalTable: "complaints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "complaint_categories",
                columns: new[] { "id", "code", "created_at", "description", "group_name", "is_active", "name", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { new Guid("32d7c462-c893-f872-cf7f-f6bab64aa2f9"), "CHEQUE_CTS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Cheque / CTS", 140, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("38da2491-c7fd-aca2-e2a8-4e6e5e7f9e5c"), "BBPS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "BBPS", 110, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3bfbe5a0-e29d-6bf1-df80-450d8b646beb"), "NEFT", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "NEFT", 150, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3efaa423-940d-4d23-97ad-9ef8a442d510"), "LOANS_ADVANCES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Loans / Advances", 130, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("43acc243-5aa7-f088-6f8d-75dadae76b98"), "ACCOUNT_SERVICES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Account Services", 200, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("579b2ff3-380c-1c83-932f-940cc4372647"), "IMPS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "IMPS", 20, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("68c852b8-bdef-081d-a039-2b39ea130edf"), "BRANCH_SERVICES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Branch Services", 250, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6b22272c-1e9a-eba8-dc4a-51668deeb904"), "OTHER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Other", 280, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("70bd8b31-f581-1695-44bc-b5eec6d1976c"), "DEPOSIT_ACCOUNTS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Deposit Accounts", 120, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7142e2d1-682c-53bf-0ed2-b2a2f74654ca"), "FRAUD", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Fraud / Suspected Fraud", 270, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("755e2770-a991-d327-0862-d5c89edda5cd"), "NACH", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "NACH", 170, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7ae478ae-781e-e285-7bca-73f3056ed012"), "INTERNET_BANKING", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "Internet Banking", 80, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("82bf17a6-6733-3043-3dc9-4609aa874e2b"), "MOBILE_BANKING", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "Mobile Banking", 90, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("91b143ed-6db4-07ba-f5a3-bca3e527c2ee"), "AEPS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "AePS", 30, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("95bb5fce-d1cc-2786-4615-0282d08ed397"), "GOVT_SCHEMES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Government Schemes", 230, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("b0bf79cc-8a5e-cb7c-af45-f3bd917b64ce"), "RTGS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "RTGS", 160, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bacd7ee0-f24a-9067-5f24-c1a6eede3504"), "UPI", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "UPI", 10, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("baea03e6-ced5-049a-e711-5e3180ca7a18"), "ECOMMERCE", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "E-Commerce", 70, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c150b910-80bd-a266-e3d1-5732de65eca3"), "PENSION", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Pension", 220, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c2ac6374-d722-7ad8-a163-4e3dabad955e"), "STAFF_BEHAVIOUR", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Staff Behaviour", 240, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c6b3f194-4667-7bf1-cd0b-92126088ebdd"), "CHARGES_FEES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Other", true, "Charges / Fees", 260, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d8f7d524-3b9a-f1e5-d130-8e724b975c93"), "QR_MERCHANT", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "QR / Merchant", 100, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e35ca180-12fe-e94d-dd76-ae4260660231"), "NFS_ATM", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "NFS / ATM", 40, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e423bc53-18d0-6618-7e77-4fe963faca7a"), "CUSTOMER_SERVICE", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Customer Service", 210, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e439761a-f5ce-e164-3f61-b1434cb0d301"), "POS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "POS", 60, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eb757c19-875c-cea4-08d8-d93663f9b6c6"), "DEBIT_CARD", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Digital Banking", true, "Debit Card", 50, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eddf26a9-1a2d-e289-1c97-3d20b132eefa"), "CASH_TRANSACTIONS", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Cash Transactions", 180, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("f8e8b14c-7422-83dd-495f-7d79993ae994"), "PASSBOOK", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Banking Services", true, "Passbook", 190, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "complaint_priorities",
                columns: new[] { "code", "is_active", "is_default", "name", "rank" },
                values: new object[,]
                {
                    { "CRITICAL", true, false, "Critical", 4 },
                    { "HIGH", true, false, "High", 3 },
                    { "LOW", true, false, "Low", 1 },
                    { "MEDIUM", true, true, "Medium", 2 }
                });

            migrationBuilder.InsertData(
                table: "complaint_statuses",
                columns: new[] { "code", "customer_label", "is_active", "is_assignment", "is_initial", "is_resolution", "is_terminal", "name", "sort_order" },
                values: new object[,]
                {
                    { "ASSIGNED", "Under review", true, true, false, false, false, "Assigned", 30 },
                    { "CLOSED", "Closed", true, false, false, false, true, "Closed", 100 },
                    { "CUSTOMER_RESPONSE", "Awaiting your response", true, false, false, false, false, "Awaiting customer response", 50 },
                    { "DUPLICATE", "Closed", true, false, false, false, true, "Duplicate", 120 },
                    { "ESCALATED", "Under process", true, false, false, false, false, "Escalated", 60 },
                    { "NEW", "Registered", true, false, true, false, false, "New", 10 },
                    { "RECEIVED", "Under review", true, false, false, false, false, "Received", 20 },
                    { "REJECTED", "Closed", true, false, false, false, true, "Rejected", 110 },
                    { "REOPENED", "Reopened", true, false, false, false, false, "Reopened", 90 },
                    { "RESOLVED", "Resolved", true, false, false, true, false, "Resolved", 80 },
                    { "TRANSFERRED", "Under process", true, false, false, false, false, "Transferred", 70 },
                    { "UNDER_PROCESS", "Under process", true, false, false, false, false, "Under process", 40 },
                    { "WITHDRAWN", "Withdrawn", true, false, false, false, true, "Withdrawn", 130 }
                });

            migrationBuilder.InsertData(
                table: "complaint_status_transitions",
                columns: new[] { "id", "from_status_code", "is_active", "requires_remark", "to_status_code" },
                values: new object[,]
                {
                    { 1, "NEW", true, false, "RECEIVED" },
                    { 2, "NEW", true, false, "ASSIGNED" },
                    { 3, "NEW", true, true, "REJECTED" },
                    { 4, "NEW", true, true, "DUPLICATE" },
                    { 5, "RECEIVED", true, false, "ASSIGNED" },
                    { 6, "RECEIVED", true, false, "UNDER_PROCESS" },
                    { 7, "RECEIVED", true, true, "TRANSFERRED" },
                    { 8, "RECEIVED", true, true, "REJECTED" },
                    { 9, "RECEIVED", true, true, "DUPLICATE" },
                    { 10, "ASSIGNED", true, false, "UNDER_PROCESS" },
                    { 11, "ASSIGNED", true, true, "TRANSFERRED" },
                    { 12, "ASSIGNED", true, true, "ESCALATED" },
                    { 13, "UNDER_PROCESS", true, true, "CUSTOMER_RESPONSE" },
                    { 14, "UNDER_PROCESS", true, true, "RESOLVED" },
                    { 15, "UNDER_PROCESS", true, true, "ESCALATED" },
                    { 16, "UNDER_PROCESS", true, true, "TRANSFERRED" },
                    { 17, "CUSTOMER_RESPONSE", true, false, "UNDER_PROCESS" },
                    { 18, "CUSTOMER_RESPONSE", true, true, "RESOLVED" },
                    { 19, "CUSTOMER_RESPONSE", true, true, "WITHDRAWN" },
                    { 20, "ESCALATED", true, false, "ASSIGNED" },
                    { 21, "ESCALATED", true, false, "UNDER_PROCESS" },
                    { 22, "ESCALATED", true, true, "RESOLVED" },
                    { 23, "TRANSFERRED", true, false, "ASSIGNED" },
                    { 24, "TRANSFERRED", true, false, "UNDER_PROCESS" },
                    { 25, "RESOLVED", true, false, "CLOSED" },
                    { 26, "RESOLVED", true, true, "REOPENED" },
                    { 27, "CLOSED", true, true, "REOPENED" },
                    { 28, "REOPENED", true, false, "ASSIGNED" },
                    { 29, "REOPENED", true, false, "UNDER_PROCESS" }
                });

            migrationBuilder.InsertData(
                table: "complaint_subcategories",
                columns: new[] { "id", "category_id", "code", "created_at", "default_department_id", "default_priority_code", "is_active", "name", "sort_order", "tat_days", "updated_at" },
                values: new object[,]
                {
                    { new Guid("0680b4dc-6444-2f8c-37f6-09769f28e786"), new Guid("c150b910-80bd-a266-e3d1-5732de65eca3"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("26bb8a4e-af91-8734-62d1-c63f088ed4ec"), new Guid("e439761a-f5ce-e164-3f61-b1434cb0d301"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3b533cf8-5c56-aeb8-3c55-6124c95d5b48"), new Guid("70bd8b31-f581-1695-44bc-b5eec6d1976c"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("3c287e16-cfc2-9697-a82a-886fef89c4ab"), new Guid("eb757c19-875c-cea4-08d8-d93663f9b6c6"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("44a6e8de-27ee-8fa2-0bf3-640b2d6307d9"), new Guid("38da2491-c7fd-aca2-e2a8-4e6e5e7f9e5c"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("4c0e458b-3fc7-0cce-bc45-f17825cdf561"), new Guid("3efaa423-940d-4d23-97ad-9ef8a442d510"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("51271bb0-f73f-5969-184d-6ba0a4d25270"), new Guid("e423bc53-18d0-6618-7e77-4fe963faca7a"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("57ac7ea8-e99c-e2ac-a9be-cef61770a586"), new Guid("bacd7ee0-f24a-9067-5f24-c1a6eede3504"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6ebcf5a6-41a3-3218-1d65-b0e706ed1510"), new Guid("eddf26a9-1a2d-e289-1c97-3d20b132eefa"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("77b8cf95-d9cf-46b5-8577-f5bafacf7ed4"), new Guid("91b143ed-6db4-07ba-f5a3-bca3e527c2ee"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("82bc93ae-f37a-e002-dc80-95c98294427c"), new Guid("d8f7d524-3b9a-f1e5-d130-8e724b975c93"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("90be7a8b-070d-930b-94c1-6345eeb947ef"), new Guid("43acc243-5aa7-f088-6f8d-75dadae76b98"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("91d5f8eb-81c3-89f8-6915-6124ba1ff349"), new Guid("7ae478ae-781e-e285-7bca-73f3056ed012"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("99f92943-7649-68f3-1146-68f9fa56d0ff"), new Guid("3bfbe5a0-e29d-6bf1-df80-450d8b646beb"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("9a210f35-22e2-d631-c9d1-01db70504a8c"), new Guid("baea03e6-ced5-049a-e711-5e3180ca7a18"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a1ca799b-be47-81ab-6563-19109dc03e54"), new Guid("68c852b8-bdef-081d-a039-2b39ea130edf"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a5190acb-f407-74e6-f622-2642cafc42d8"), new Guid("7142e2d1-682c-53bf-0ed2-b2a2f74654ca"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a7c33eb8-d87b-ffa3-6cfd-4dc2ce6ad114"), new Guid("c6b3f194-4667-7bf1-cd0b-92126088ebdd"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aabccea1-5710-18d7-c327-3ee28523b29f"), new Guid("f8e8b14c-7422-83dd-495f-7d79993ae994"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ac858dfd-0b01-01ea-28f8-efbbdc5c8436"), new Guid("b0bf79cc-8a5e-cb7c-af45-f3bd917b64ce"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bb79fa0c-a117-9c27-f2c3-7d2d9a09480d"), new Guid("c2ac6374-d722-7ad8-a163-4e3dabad955e"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("be6477e5-d7b7-0932-e837-04070bfd90eb"), new Guid("95bb5fce-d1cc-2786-4615-0282d08ed397"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c37d81ab-2793-26e1-0615-b0496d968ed6"), new Guid("32d7c462-c893-f872-cf7f-f6bab64aa2f9"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("c8fa8068-635a-0439-a1cc-f931ed8ad8b1"), new Guid("e35ca180-12fe-e94d-dd76-ae4260660231"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d790c96d-cb30-e949-f63e-6893b9d13051"), new Guid("bacd7ee0-f24a-9067-5f24-c1a6eede3504"), "FAILED_TRANSACTION", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "Amount debited but transaction failed", 10, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e6f6869f-cfaa-b60f-892d-9343b559d322"), new Guid("82bf17a6-6733-3043-3dc9-4609aa874e2b"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("e84973d5-c6ab-a648-ad90-6be8cde968ea"), new Guid("579b2ff3-380c-1c83-932f-940cc4372647"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("fc381832-9366-72db-8055-acc91ff7b91d"), new Guid("755e2770-a991-d327-0862-d5c89edda5cd"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("fe7dc32d-97f2-c7d4-d8de-9a67d45d59b8"), new Guid("6b22272c-1e9a-eba8-dc4a-51668deeb904"), "GENERAL", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, true, "General", 100, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_role_mapping_iam_role_application_role",
                table: "application_role_mapping",
                columns: new[] { "iam_role", "application_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_module_record_id",
                table: "audit_logs",
                columns: new[] { "module", "record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_branches_code",
                table: "branches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_region_id",
                table: "branches",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_assignments_assigned_department_id",
                table: "complaint_assignments",
                column: "assigned_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_assignments_complaint_id_assigned_at",
                table: "complaint_assignments",
                columns: new[] { "complaint_id", "assigned_at" });

            migrationBuilder.CreateIndex(
                name: "ix_complaint_attachments_complaint_id",
                table: "complaint_attachments",
                column: "complaint_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_categories_code",
                table: "complaint_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaint_remarks_complaint_id_created_at",
                table: "complaint_remarks",
                columns: new[] { "complaint_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_complaint_status_history_complaint_id_changed_at",
                table: "complaint_status_history",
                columns: new[] { "complaint_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_complaint_status_transitions_from_status_code_to_status_code",
                table: "complaint_status_transitions",
                columns: new[] { "from_status_code", "to_status_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaint_status_transitions_to_status_code",
                table: "complaint_status_transitions",
                column: "to_status_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_category_id_code",
                table: "complaint_subcategories",
                columns: new[] { "category_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_default_department_id",
                table: "complaint_subcategories",
                column: "default_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_default_priority_code",
                table: "complaint_subcategories",
                column: "default_priority_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_account_number",
                table: "complaints",
                column: "account_number");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_assigned_department_id",
                table: "complaints",
                column: "assigned_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_assigned_employee_id",
                table: "complaints",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_branch_id",
                table: "complaints",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_category_id",
                table: "complaints",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_closed_at_sla_due_date",
                table: "complaints",
                columns: new[] { "closed_at", "sla_due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_complaint_number",
                table: "complaints",
                column: "complaint_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_created_at",
                table: "complaints",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_mobile_number",
                table: "complaints",
                column: "mobile_number");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_priority_code",
                table: "complaints",
                column: "priority_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_status_code",
                table: "complaints",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_sub_category_id",
                table: "complaints",
                column: "sub_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_transaction_id",
                table: "complaints",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_departments_code",
                table: "departments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_regions_code",
                table: "regions",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_role_mapping");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "complaint_assignments");

            migrationBuilder.DropTable(
                name: "complaint_attachments");

            migrationBuilder.DropTable(
                name: "complaint_number_sequences");

            migrationBuilder.DropTable(
                name: "complaint_remarks");

            migrationBuilder.DropTable(
                name: "complaint_status_history");

            migrationBuilder.DropTable(
                name: "complaint_status_transitions");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "complaint_statuses");

            migrationBuilder.DropTable(
                name: "complaint_subcategories");

            migrationBuilder.DropTable(
                name: "regions");

            migrationBuilder.DropTable(
                name: "complaint_categories");

            migrationBuilder.DropTable(
                name: "complaint_priorities");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
