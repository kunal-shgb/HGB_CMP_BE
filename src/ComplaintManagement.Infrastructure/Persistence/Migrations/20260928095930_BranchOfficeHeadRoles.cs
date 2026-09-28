using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ComplaintManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BranchOfficeHeadRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("4f8a4b00-80d3-2321-b3ab-7c6a82020de6"));

            migrationBuilder.UpdateData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("03a1bc9c-a4b4-3e70-0243-1219fc510e93"),
                column: "office_type",
                value: "Regional Office");

            migrationBuilder.UpdateData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("c8adcc93-d5ba-b281-e87e-ad90154c4925"),
                column: "office_type",
                value: "Regional Office");

            migrationBuilder.InsertData(
                table: "application_role_mapping",
                columns: new[] { "id", "application_role", "created_at", "iam_role", "is_active", "office_type", "updated_at" },
                values: new object[,]
                {
                    { new Guid("112f5ce1-b755-473f-bbda-a23592f68e00"), "OFFICE_HEAD", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "OfficeHead", true, "Branch", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("31cdf8ea-7f51-d961-291f-2f85916bc92a"), "CHECKER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Checker", true, "Head Office", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("5bb4ff7b-513f-d623-26a2-73a2c89d70a8"), "MAKER", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Maker", true, "Head Office", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("112f5ce1-b755-473f-bbda-a23592f68e00"));

            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("31cdf8ea-7f51-d961-291f-2f85916bc92a"));

            migrationBuilder.DeleteData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("5bb4ff7b-513f-d623-26a2-73a2c89d70a8"));

            migrationBuilder.UpdateData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("03a1bc9c-a4b4-3e70-0243-1219fc510e93"),
                column: "office_type",
                value: null);

            migrationBuilder.UpdateData(
                table: "application_role_mapping",
                keyColumn: "id",
                keyValue: new Guid("c8adcc93-d5ba-b281-e87e-ad90154c4925"),
                column: "office_type",
                value: null);

            migrationBuilder.InsertData(
                table: "application_role_mapping",
                columns: new[] { "id", "application_role", "created_at", "iam_role", "is_active", "office_type", "updated_at" },
                values: new object[] { new Guid("4f8a4b00-80d3-2321-b3ab-7c6a82020de6"), "ADMIN", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", true, null, new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
