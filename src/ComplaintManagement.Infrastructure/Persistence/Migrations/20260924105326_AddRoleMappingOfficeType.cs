using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleMappingOfficeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_application_role_mapping_iam_role_application_role",
                table: "application_role_mapping");

            migrationBuilder.AddColumn<string>(
                name: "office_type",
                table: "application_role_mapping",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_application_role_mapping_iam_role_office_type_application_r~",
                table: "application_role_mapping",
                columns: new[] { "iam_role", "office_type", "application_role" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_application_role_mapping_iam_role_office_type_application_r~",
                table: "application_role_mapping");

            migrationBuilder.DropColumn(
                name: "office_type",
                table: "application_role_mapping");

            migrationBuilder.CreateIndex(
                name: "ix_application_role_mapping_iam_role_application_role",
                table: "application_role_mapping",
                columns: new[] { "iam_role", "application_role" },
                unique: true);
        }
    }
}
