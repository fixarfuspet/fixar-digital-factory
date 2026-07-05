using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStationAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StationAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProducedPairs = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationAssignments_InjectionStations_InjectionStationId",
                        column: x => x.InjectionStationId,
                        principalTable: "InjectionStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StationAssignments_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StationAssignments_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignments_InjectionStationId",
                table: "StationAssignments",
                column: "InjectionStationId");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignments_MoldId",
                table: "StationAssignments",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignments_OrderItemId",
                table: "StationAssignments",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationAssignments");
        }
    }
}
