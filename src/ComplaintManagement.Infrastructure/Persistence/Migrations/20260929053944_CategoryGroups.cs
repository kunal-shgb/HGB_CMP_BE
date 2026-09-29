using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoryGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: turns the free-text complaint_categories.group_name into rows of
            // complaint_category_groups, keeping any group an admin had typed, then links categories by id.
            migrationBuilder.CreateTable(
                name: "complaint_category_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaint_category_groups", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "complaint_category_groups",
                columns: new[] { "id", "code", "created_at", "is_active", "name", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { new Guid("0b0cf104-76f2-31df-4b5f-42bce76cc404"), "BANKING_SERVICES", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Banking Services", 20, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("49704117-8df7-2dfd-e536-069249100cd2"), "DIGITAL_BANKING", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Digital Banking", 10, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("d79b2d5e-c0bf-c5e6-84aa-b2d1b1138675"), "OTHER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Other", 30, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            // Groups typed by admins that are not among the seeded ones. Codes follow ReferenceSeed.GroupCode.
            migrationBuilder.Sql("""
                INSERT INTO complaint_category_groups (id, code, name, sort_order, is_active, created_at, updated_at)
                SELECT gen_random_uuid(), x.code, min(x.group_name), 100 + 10 * row_number() OVER (ORDER BY x.code), true, now(), now()
                FROM (
                    SELECT group_name,
                           left(coalesce(nullif(trim(both '_' from regexp_replace(upper(group_name), '[^A-Z0-9]+', '_', 'g')), ''), 'GROUP'), 40) AS code
                    FROM complaint_categories
                ) x
                WHERE NOT EXISTS (SELECT 1 FROM complaint_category_groups g WHERE g.code = x.code)
                GROUP BY x.code;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "complaint_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE complaint_categories c
                SET group_id = g.id
                FROM complaint_category_groups g
                WHERE g.code = left(coalesce(nullif(trim(both '_' from regexp_replace(upper(c.group_name), '[^A-Z0-9]+', '_', 'g')), ''), 'GROUP'), 40);
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "complaint_categories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "group_name",
                table: "complaint_categories");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_categories_group_id",
                table: "complaint_categories",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_complaint_category_groups_code",
                table: "complaint_category_groups",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_complaint_categories_complaint_category_groups_group_id",
                table: "complaint_categories",
                column: "group_id",
                principalTable: "complaint_category_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_name",
                table: "complaint_categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE complaint_categories c SET group_name = g.name
                FROM complaint_category_groups g WHERE g.id = c.group_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_complaint_categories_complaint_category_groups_group_id",
                table: "complaint_categories");

            migrationBuilder.DropIndex(
                name: "ix_complaint_categories_group_id",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "complaint_categories");

            migrationBuilder.DropTable(
                name: "complaint_category_groups");
        }
    }
}
