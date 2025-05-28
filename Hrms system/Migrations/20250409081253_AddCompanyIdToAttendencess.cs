using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToAttendencess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_CompanyId",
                table: "Attendance",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Companies_CompanyId",
                table: "Attendance",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Companies_CompanyId",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_CompanyId",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Attendance");
        }
    }
}
