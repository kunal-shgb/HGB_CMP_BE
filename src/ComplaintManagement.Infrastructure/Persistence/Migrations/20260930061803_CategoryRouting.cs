using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoryRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "escalated_division_code",
                table: "complaints",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "escalated_division_name",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "to_division_code",
                table: "complaint_escalations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "to_division_name",
                table: "complaint_escalations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "direct_to_head_office",
                table: "complaint_categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ho_division_code",
                table: "complaint_categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ro_division_code",
                table: "complaint_categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("32d7c462-c893-f872-cf7f-f6bab64aa2f9"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("38da2491-c7fd-aca2-e2a8-4e6e5e7f9e5c"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("3bfbe5a0-e29d-6bf1-df80-450d8b646beb"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("3efaa423-940d-4d23-97ad-9ef8a442d510"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("43acc243-5aa7-f088-6f8d-75dadae76b98"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("579b2ff3-380c-1c83-932f-940cc4372647"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("68c852b8-bdef-081d-a039-2b39ea130edf"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("6b22272c-1e9a-eba8-dc4a-51668deeb904"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("70bd8b31-f581-1695-44bc-b5eec6d1976c"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("7142e2d1-682c-53bf-0ed2-b2a2f74654ca"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("755e2770-a991-d327-0862-d5c89edda5cd"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("7ae478ae-781e-e285-7bca-73f3056ed012"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("82bf17a6-6733-3043-3dc9-4609aa874e2b"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("91b143ed-6db4-07ba-f5a3-bca3e527c2ee"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("95bb5fce-d1cc-2786-4615-0282d08ed397"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("b0bf79cc-8a5e-cb7c-af45-f3bd917b64ce"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("bacd7ee0-f24a-9067-5f24-c1a6eede3504"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("baea03e6-ced5-049a-e711-5e3180ca7a18"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c150b910-80bd-a266-e3d1-5732de65eca3"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c2ac6374-d722-7ad8-a163-4e3dabad955e"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("c6b3f194-4667-7bf1-cd0b-92126088ebdd"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("d8f7d524-3b9a-f1e5-d130-8e724b975c93"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e35ca180-12fe-e94d-dd76-ae4260660231"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e423bc53-18d0-6618-7e77-4fe963faca7a"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("e439761a-f5ce-e164-3f61-b1434cb0d301"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("eb757c19-875c-cea4-08d8-d93663f9b6c6"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("eddf26a9-1a2d-e289-1c97-3d20b132eefa"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "complaint_categories",
                keyColumn: "id",
                keyValue: new Guid("f8e8b14c-7422-83dd-495f-7d79993ae994"),
                columns: new[] { "direct_to_head_office", "ho_division_code", "ro_division_code" },
                values: new object[] { false, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "escalated_division_code",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "escalated_division_name",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "to_division_code",
                table: "complaint_escalations");

            migrationBuilder.DropColumn(
                name: "to_division_name",
                table: "complaint_escalations");

            migrationBuilder.DropColumn(
                name: "direct_to_head_office",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "ho_division_code",
                table: "complaint_categories");

            migrationBuilder.DropColumn(
                name: "ro_division_code",
                table: "complaint_categories");
        }
    }
}
