using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Announcements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CompanyId",
                table: "Announcements",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Companies_CompanyId",
                table: "Announcements",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Companies_CompanyId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_CompanyId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Announcements");
        }
    }
}
