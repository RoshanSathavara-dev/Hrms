using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdToAttendence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EmployeeId",
                table: "Attendance",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Employees_EmployeeId",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_EmployeeId",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Attendance");
        }
    }
}
