using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComplaintTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "complaints",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            // Hand-edited: complaints lodged before titles existed get the first sentence of their description.
            migrationBuilder.Sql("""
                UPDATE complaints
                SET title = left(coalesce(nullif(trim(split_part(description, '.', 1)), ''), 'Complaint'), 150)
                WHERE title = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                table: "complaints");
        }
    }
}
