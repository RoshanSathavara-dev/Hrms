using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkWeekRuleIdToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkWeekRuleId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_WorkWeekRuleId",
                table: "Employees",
                column: "WorkWeekRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_WorkWeekRules_WorkWeekRuleId",
                table: "Employees",
                column: "WorkWeekRuleId",
                principalTable: "WorkWeekRules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_WorkWeekRules_WorkWeekRuleId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_WorkWeekRuleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkWeekRuleId",
                table: "Employees");
        }
    }
}
