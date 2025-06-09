using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class AddLossOfPayLeaveTypein : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLossOfPay",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLossOfPay",
                table: "LeaveTypes");
        }
    }
}
