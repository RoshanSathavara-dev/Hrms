using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddStartAndEndLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFirstHalfEnd",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "IsFirstHalfStart",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "IsHalfDayEnd",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "IsHalfDayStart",
                table: "LeaveRequests");

            migrationBuilder.AddColumn<string>(
                name: "EndHalf",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartHalf",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndHalf",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "StartHalf",
                table: "LeaveRequests");

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstHalfEnd",
                table: "LeaveRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFirstHalfStart",
                table: "LeaveRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalfDayEnd",
                table: "LeaveRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalfDayStart",
                table: "LeaveRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
