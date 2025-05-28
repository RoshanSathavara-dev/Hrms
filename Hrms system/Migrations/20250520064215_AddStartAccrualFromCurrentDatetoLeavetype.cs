using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddStartAccrualFromCurrentDatetoLeavetype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualPeriodEnd",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AccrualPeriodStart",
                table: "LeaveTypes");

            migrationBuilder.AddColumn<bool>(
                name: "StartAccrualFromCurrentDate",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartAccrualFromCurrentDate",
                table: "LeaveTypes");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccrualPeriodEnd",
                table: "LeaveTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccrualPeriodStart",
                table: "LeaveTypes",
                type: "datetime2",
                nullable: true);
        }
    }
}
