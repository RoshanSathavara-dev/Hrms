using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployeeCompanyRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId1",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId1",
                table: "Employees",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Company_CompanyId1",
                table: "Employees",
                column: "CompanyId1",
                principalTable: "Company",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Company_CompanyId1",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId1",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "Employees");
        }
    }
}
