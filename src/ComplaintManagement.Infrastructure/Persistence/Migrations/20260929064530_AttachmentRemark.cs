using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentRemark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "remark_id",
                table: "complaint_attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaint_attachments_remark_id",
                table: "complaint_attachments",
                column: "remark_id");

            migrationBuilder.AddForeignKey(
                name: "fk_complaint_attachments_complaint_remarks_remark_id",
                table: "complaint_attachments",
                column: "remark_id",
                principalTable: "complaint_remarks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_complaint_attachments_complaint_remarks_remark_id",
                table: "complaint_attachments");

            migrationBuilder.DropIndex(
                name: "ix_complaint_attachments_remark_id",
                table: "complaint_attachments");

            migrationBuilder.DropColumn(
                name: "remark_id",
                table: "complaint_attachments");
        }
    }
}
