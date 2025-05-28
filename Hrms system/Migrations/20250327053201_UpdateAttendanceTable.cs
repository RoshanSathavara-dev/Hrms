using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance");

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakEnd",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakStart",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "BreakEnd",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "BreakStart",
                table: "Attendance");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_AspNetUsers_UserId",
                table: "Attendance",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
