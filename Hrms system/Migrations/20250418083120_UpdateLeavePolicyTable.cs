using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeavePolicyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "TotalLeavesPerYear",
                table: "LeavePolicies");

            migrationBuilder.RenameColumn(
                name: "LeaveType",
                table: "LeavePolicies",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "IsCarryForwardAllowed",
                table: "LeavePolicies",
                newName: "LeaveEncash");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "LeavePolicies",
                newName: "CreditOnPresentDayBasis");

            migrationBuilder.RenameColumn(
                name: "AllowedDuringNoticePeriod",
                table: "LeavePolicies",
                newName: "CreditAccrual");

            migrationBuilder.AddColumn<string>(
                name: "AccrualFrequency",
                table: "LeavePolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccrualPeriod",
                table: "LeavePolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowedUnderNoticePeriod",
                table: "LeavePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowedUnderProbation",
                table: "LeavePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CarryForward",
                table: "LeavePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LeavePolicies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "LeaveCount",
                table: "LeavePolicies",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualFrequency",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "AccrualPeriod",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "AllowedUnderNoticePeriod",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "AllowedUnderProbation",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "CarryForward",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LeavePolicies");

            migrationBuilder.DropColumn(
                name: "LeaveCount",
                table: "LeavePolicies");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "LeavePolicies",
                newName: "LeaveType");

            migrationBuilder.RenameColumn(
                name: "LeaveEncash",
                table: "LeavePolicies",
                newName: "IsCarryForwardAllowed");

            migrationBuilder.RenameColumn(
                name: "CreditOnPresentDayBasis",
                table: "LeavePolicies",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "CreditAccrual",
                table: "LeavePolicies",
                newName: "AllowedDuringNoticePeriod");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LeavePolicies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TotalLeavesPerYear",
                table: "LeavePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
