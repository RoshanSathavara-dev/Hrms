using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hrms_system.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLeavePolicyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeavePolicies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeavePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccrualFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccrualPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedUnderNoticePeriod = table.Column<bool>(type: "bit", nullable: false),
                    AllowedUnderProbation = table.Column<bool>(type: "bit", nullable: false),
                    CarryForward = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreditAccrual = table.Column<bool>(type: "bit", nullable: false),
                    CreditOnPresentDayBasis = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeaveCount = table.Column<double>(type: "float", nullable: false),
                    LeaveEncash = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicies", x => x.Id);
                });
        }
    }
}
