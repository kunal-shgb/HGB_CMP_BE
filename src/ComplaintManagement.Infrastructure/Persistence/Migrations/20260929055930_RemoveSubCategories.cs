using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Order: add the category columns, copy values across, then drop sub-categories.
            migrationBuilder.AddColumn<string>(
                name: "default_department_code",
                table: "complaint_categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_priority_code",
                table: "complaint_categories",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tat_days",
                table: "complaint_categories",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("32d7c462-c893-f872-cf7f-f6bab64aa2f9"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("38da2491-c7fd-aca2-e2a8-4e6e5e7f9e5c"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("3bfbe5a0-e29d-6bf1-df80-450d8b646beb"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("3efaa423-940d-4d23-97ad-9ef8a442d510"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("43acc243-5aa7-f088-6f8d-75dadae76b98"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("579b2ff3-380c-1c83-932f-940cc4372647"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("68c852b8-bdef-081d-a039-2b39ea130edf"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("6b22272c-1e9a-eba8-dc4a-51668deeb904"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("70bd8b31-f581-1695-44bc-b5eec6d1976c"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("7142e2d1-682c-53bf-0ed2-b2a2f74654ca"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("755e2770-a991-d327-0862-d5c89edda5cd"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("7ae478ae-781e-e285-7bca-73f3056ed012"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("82bf17a6-6733-3043-3dc9-4609aa874e2b"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("91b143ed-6db4-07ba-f5a3-bca3e527c2ee"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("95bb5fce-d1cc-2786-4615-0282d08ed397"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("b0bf79cc-8a5e-cb7c-af45-f3bd917b64ce"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("bacd7ee0-f24a-9067-5f24-c1a6eede3504"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("baea03e6-ced5-049a-e711-5e3180ca7a18"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c150b910-80bd-a266-e3d1-5732de65eca3"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c2ac6374-d722-7ad8-a163-4e3dabad955e"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c6b3f194-4667-7bf1-cd0b-92126088ebdd"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("d8f7d524-3b9a-f1e5-d130-8e724b975c93"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e35ca180-12fe-e94d-dd76-ae4260660231"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e423bc53-18d0-6618-7e77-4fe963faca7a"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e439761a-f5ce-e164-3f61-b1434cb0d301"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("eb757c19-875c-cea4-08d8-d93663f9b6c6"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("eddf26a9-1a2d-e289-1c97-3d20b132eefa"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("f8e8b14c-7422-83dd-495f-7d79993ae994"),
                columns: new[] { "default_department_code", "default_priority_code", "tat_days" },
                values: new object[] { null, null, null });

            // Hand-edited: each category takes its TAT, default priority and department from its "General"
            // sub-category, or its first one, before sub-categories are dropped. Runs after the seed updates above.
            migrationBuilder.Sql("""
                UPDATE complaint_categories c
                SET tat_days = s.tat_days,
                    default_priority_code = s.default_priority_code,
                    default_department_code = s.default_department_code
                FROM (
                    SELECT DISTINCT ON (category_id) category_id, tat_days, default_priority_code, default_department_code
                    FROM complaint_subcategories
                    ORDER BY category_id, (code = 'GENERAL') DESC, sort_order, name
                ) s
                WHERE s.category_id = c.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_complaints_complaint_subcategories_sub_category_id",
                table: "complaints");

            migrationBuilder.DropTable(
                name: "complaint_subcategories");

            migrationBuilder.DropIndex(
                name: "ix_complaints_sub_category_id",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "sub_category_id",
                table: "complaints");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_categories_default_priority_code",
                table: "complaint_categories",
                column: "default_priority_code");

            migrationBuilder.AddForeignKey(
                name: "fk_complaint_categories_complaint_priorities_default_priority_~",
                table: "complaint_categories",
                column: "default_priority_code",
                principalTable: "complaint_priorities",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: recreates one "General" sub-category per category, carrying the category's
            // TAT, priority and department back, and points every complaint at it.
            migrationBuilder.CreateTable(
                name: "complaint_subcategories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    default_department_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_priority_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    tat_days = table.Column<int>(type: "integer", nullable: true),
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
                });

            migrationBuilder.Sql("""
                INSERT INTO complaint_subcategories
                    (id, category_id, code, name, tat_days, default_priority_code, default_department_code, sort_order, is_active, created_at, updated_at)
                SELECT gen_random_uuid(), id, 'GENERAL', 'General', tat_days, default_priority_code, default_department_code, 100, true, now(), now()
                FROM complaint_categories;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "sub_category_id",
                table: "complaints",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE complaints c SET sub_category_id = s.id
                FROM complaint_subcategories s WHERE s.category_id = c.category_id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "sub_category_id",
                table: "complaints",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_sub_category_id",
                table: "complaints",
                column: "sub_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_category_id_code",
                table: "complaint_subcategories",
                columns: new[] { "category_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaint_subcategories_default_priority_code",
                table: "complaint_subcategories",
                column: "default_priority_code");

            migrationBuilder.AddForeignKey(
                name: "fk_complaints_complaint_subcategories_sub_category_id",
                table: "complaints",
                column: "sub_category_id",
                principalTable: "complaint_subcategories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "fk_complaint_categories_complaint_priorities_default_priority_~",
                table: "complaint_categories");

            migrationBuilder.DropIndex(
                name: "ix_complaint_categories_default_priority_code",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "default_department_code",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "default_priority_code",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "tat_days",
                table: "complaint_categories");
        }
    }
}
