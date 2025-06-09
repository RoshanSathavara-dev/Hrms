using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class addWeeklyPatternJsontoworkweek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Friday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Monday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Saturday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Sunday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Thursday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Tuesday",
                table: "WorkWeekRules");

            migrationBuilder.DropColumn(
                name: "Wednesday",
                table: "WorkWeekRules");

            migrationBuilder.AddColumn<string>(
                name: "WeeklyPatternJson",
                table: "WorkWeekRules",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeeklyPatternJson",
                table: "WorkWeekRules");

            migrationBuilder.AddColumn<bool>(
                name: "Friday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Monday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Saturday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Sunday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Thursday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Tuesday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Wednesday",
                table: "WorkWeekRules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
