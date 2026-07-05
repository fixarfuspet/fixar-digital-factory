using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStationNumberSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StationNumberSnapshot",
                table: "StationAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StationNumberSnapshot",
                table: "StationAssignments");
        }
    }
}
