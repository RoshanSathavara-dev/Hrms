using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoClockOutToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoClockOutScheduled",
                table: "Attendance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoClockOut",
                table: "Attendance",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoClockOutScheduled",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IsAutoClockOut",
                table: "Attendance");
        }
    }
}
