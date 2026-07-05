using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInjectionStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuttingMachines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MachineType = table.Column<string>(type: "text", nullable: false),
                    OperatorName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingMachines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Molds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    SizeRange = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Molds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CuttingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuttingMachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CutPairs = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuttingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuttingRecords_CuttingMachines_CuttingMachineId",
                        column: x => x.CuttingMachineId,
                        principalTable: "CuttingMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CuttingRecords_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InjectionStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentMoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMoldChangeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InjectionStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InjectionStations_Molds_CurrentMoldId",
                        column: x => x.CurrentMoldId,
                        principalTable: "Molds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InjectionStations_Orders_CurrentOrderId",
                        column: x => x.CurrentOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProducedPairs = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_InjectionStations_InjectionStationId",
                        column: x => x.InjectionStationId,
                        principalTable: "InjectionStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionRecords_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_CuttingMachineId",
                table: "CuttingRecords",
                column: "CuttingMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_OrderId",
                table: "CuttingRecords",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectionStations_CurrentMoldId",
                table: "InjectionStations",
                column: "CurrentMoldId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectionStations_CurrentOrderId",
                table: "InjectionStations",
                column: "CurrentOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_InjectionStationId",
                table: "ProductionRecords",
                column: "InjectionStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_MoldId",
                table: "ProductionRecords",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecords_OrderId",
                table: "ProductionRecords",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuttingRecords");

            migrationBuilder.DropTable(
                name: "ProductionRecords");

            migrationBuilder.DropTable(
                name: "CuttingMachines");

            migrationBuilder.DropTable(
                name: "InjectionStations");

            migrationBuilder.DropTable(
                name: "Molds");
        }
    }
}
