using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class Removeexpireleave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarryForwardExpiryInMonths",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CarryForwardExpiry",
                table: "EmployeeLeaveBalances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarryForwardExpiryInMonths",
                table: "LeaveTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CarryForwardExpiry",
                table: "EmployeeLeaveBalances",
                type: "datetime2",
                nullable: true);
        }
    }
}
