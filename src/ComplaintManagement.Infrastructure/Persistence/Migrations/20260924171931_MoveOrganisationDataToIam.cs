using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveOrganisationDataToIam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assigned_department_code",
                table: "complaints",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "assigned_department_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                table: "complaints",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "branch_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "region_code",
                table: "complaints",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "region_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "default_department_code",
                table: "complaint_subcategories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "assigned_department_code",
                table: "complaint_assignments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Keep each complaint's office before the local organisation tables go: from now on
            // organisation data comes from the Bank IAM and complaints hold a snapshot of it.
            migrationBuilder.Sql(@"
UPDATE complaints c
SET branch_code = b.code, branch_name = b.name, region_code = r.code, region_name = r.name
FROM branches b JOIN regions r ON r.id = b.region_id
WHERE b.id = c.branch_id;

UPDATE complaints c
SET assigned_department_code = d.code, assigned_department_name = d.name
FROM departments d
WHERE d.id = c.assigned_department_id;

UPDATE complaint_subcategories s
SET default_department_code = d.code
FROM departments d
WHERE d.id = s.default_department_id;

UPDATE complaint_assignments a
SET assigned_department_code = d.code
FROM departments d
WHERE d.id = a.assigned_department_id;
");

            migrationBuilder.DropForeignKey(
                name: "fk_complaint_assignments_departments_assigned_department_id",
                table: "complaint_assignments");
            migrationBuilder.DropForeignKey(
                name: "fk_complaint_subcategories_departments_default_department_id",
                table: "complaint_subcategories");
            migrationBuilder.DropForeignKey(
                name: "fk_complaints_branches_branch_id",
                table: "complaints");
            migrationBuilder.DropForeignKey(
                name: "fk_complaints_departments_assigned_department_id",
                table: "complaints");
            migrationBuilder.DropTable(
                name: "branches");
            migrationBuilder.DropTable(
                name: "departments");
            migrationBuilder.DropTable(
                name: "regions");
            migrationBuilder.DropIndex(
                name: "ix_complaints_assigned_department_id",
                table: "complaints");
            migrationBuilder.DropIndex(
                name: "ix_complaints_branch_id",
                table: "complaints");
            migrationBuilder.DropIndex(
                name: "ix_complaint_subcategories_default_department_id",
                table: "complaint_subcategories");
            migrationBuilder.DropIndex(
                name: "ix_complaint_assignments_assigned_department_id",
                table: "complaint_assignments");
            migrationBuilder.DropColumn(
                name: "assigned_department_id",
                table: "complaints");
            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "complaints");
            migrationBuilder.DropColumn(
                name: "default_department_id",
                table: "complaint_subcategories");
            migrationBuilder.DropColumn(
                name: "assigned_department_id",
                table: "complaint_assignments");
            migrationBuilder.CreateIndex(
                name: "ix_complaints_assigned_department_code",
                table: "complaints",
                column: "assigned_department_code");
            migrationBuilder.CreateIndex(
                name: "ix_complaints_branch_code",
                table: "complaints",
                column: "branch_code");
            migrationBuilder.CreateIndex(
                name: "ix_complaints_region_code",
                table: "complaints",
                column: "region_code");
        
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_complaints_assigned_department_code",
                table: "complaints");

            migrationBuilder.DropIndex(
                name: "ix_complaints_branch_code",
                table: "complaints");

            migrationBuilder.DropIndex(
                name: "ix_complaints_region_code",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "assigned_department_code",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "assigned_department_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "branch_code",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "branch_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "region_code",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "region_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "default_department_code",
                table: "complaint_subcategories");

            migrationBuilder.DropColumn(
                name: "assigned_department_code",
                table: "complaint_assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_department_id",
                table: "complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "complaints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "default_department_id",
                table: "complaint_subcategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_department_id",
                table: "complaint_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("0680b4dc-6444-2f8c-37f6-09769f28e786"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("26bb8a4e-af91-8734-62d1-c63f088ed4ec"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("3b533cf8-5c56-aeb8-3c55-6124c95d5b48"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("3c287e16-cfc2-9697-a82a-886fef89c4ab"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("44a6e8de-27ee-8fa2-0bf3-640b2d6307d9"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("4c0e458b-3fc7-0cce-bc45-f17825cdf561"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("51271bb0-f73f-5969-184d-6ba0a4d25270"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("57ac7ea8-e99c-e2ac-a9be-cef61770a586"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("6ebcf5a6-41a3-3218-1d65-b0e706ed1510"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("77b8cf95-d9cf-46b5-8577-f5bafacf7ed4"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("82bc93ae-f37a-e002-dc80-95c98294427c"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("90be7a8b-070d-930b-94c1-6345eeb947ef"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("91d5f8eb-81c3-89f8-6915-6124ba1ff349"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("99f92943-7649-68f3-1146-68f9fa56d0ff"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("9a210f35-22e2-d631-c9d1-01db70504a8c"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("a1ca799b-be47-81ab-6563-19109dc03e54"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("a5190acb-f407-74e6-f622-2642cafc42d8"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("a7c33eb8-d87b-ffa3-6cfd-4dc2ce6ad114"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("aabccea1-5710-18d7-c327-3ee28523b29f"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("ac858dfd-0b01-01ea-28f8-efbbdc5c8436"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("bb79fa0c-a117-9c27-f2c3-7d2d9a09480d"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("be6477e5-d7b7-0932-e837-04070bfd90eb"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("c37d81ab-2793-26e1-0615-b0496d968ed6"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("c8fa8068-635a-0439-a1cc-f931ed8ad8b1"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("d790c96d-cb30-e949-f63e-6893b9d13051"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("e6f6869f-cfaa-b60f-892d-9343b559d322"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("e84973d5-c6ab-a648-ad90-6be8cde968ea"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("fc381832-9366-72db-8055-acc91ff7b91d"),
                column: "default_department_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "complaint_subcategories",
                keyColumn: "id",
                keyValue: new Guid("fe7dc32d-97f2-c7d4-d8de-9a67d45d59b8"),
                column: "default_department_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_assigned_department_id",
                table: "complaints",
                column: "assigned_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_branch_id",
                table: "complaints",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_default_department_id",
                table: "complaint_subcategories",
                column: "default_department_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_assignments_assigned_department_id",
                table: "complaint_assignments",
                column: "assigned_department_id");

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
                name: "ix_departments_code",
                table: "departments",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_regions_code",
                table: "regions",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_complaint_assignments_departments_assigned_department_id",
                table: "complaint_assignments",
                column: "assigned_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_complaint_subcategories_departments_default_department_id",
                table: "complaint_subcategories",
                column: "default_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_complaints_branches_branch_id",
                table: "complaints",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_complaints_departments_assigned_department_id",
                table: "complaints",
                column: "assigned_department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
