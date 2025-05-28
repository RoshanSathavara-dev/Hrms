using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToHoliday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Holidays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_CompanyId",
                table: "Holidays",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_Companies_CompanyId",
                table: "Holidays",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_Companies_CompanyId",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_CompanyId",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Holidays");
        }
    }
}
