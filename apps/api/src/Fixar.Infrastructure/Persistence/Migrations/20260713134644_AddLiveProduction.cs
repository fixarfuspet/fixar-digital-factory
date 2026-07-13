using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionNumber = table.Column<string>(type: "text", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkOrderNumber = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCode = table.Column<string>(type: "text", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<string>(type: "text", nullable: true),
                    FoamType = table.Column<string>(type: "text", nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineCode = table.Column<string>(type: "text", nullable: false),
                    MachineName = table.Column<string>(type: "text", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorCode = table.Column<string>(type: "text", nullable: false),
                    OperatorName = table.Column<string>(type: "text", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlannedPairs = table.Column<long>(type: "bigint", nullable: false),
                    ProducedPairs = table.Column<long>(type: "bigint", nullable: false),
                    GoodPairs = table.Column<long>(type: "bigint", nullable: false),
                    FirePairs = table.Column<long>(type: "bigint", nullable: false),
                    TargetPairWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualAveragePairWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualAverageDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetCuringTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActualAverageCuringTimeSeconds = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetCycleTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActualAverageCycleTimeSeconds = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalCycleCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalDowntimeMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalRunningMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedMaterialCost = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualMaterialCost = table.Column<decimal>(type: "numeric", nullable: true),
                    ProductionNote = table.Column<string>(type: "text", nullable: true),
                    QualityNote = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSessions_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionSessions_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionSessions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationNumber = table.Column<int>(type: "integer", nullable: false),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldCode = table.Column<string>(type: "text", nullable: true),
                    MoldName = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCode = table.Column<string>(type: "text", nullable: true),
                    ProductName = table.Column<string>(type: "text", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorCode = table.Column<string>(type: "text", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CurrentCycleNumber = table.Column<long>(type: "bigint", nullable: false),
                    ProducedPairs = table.Column<long>(type: "bigint", nullable: false),
                    GoodPairs = table.Column<long>(type: "bigint", nullable: false),
                    FirePairs = table.Column<long>(type: "bigint", nullable: false),
                    LastCycleStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCycleCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CuringStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CuringExpectedEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReleaseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CyclesSinceLastRelease = table.Column<int>(type: "integer", nullable: false),
                    ReleaseFrequencyCycles = table.Column<int>(type: "integer", nullable: true),
                    TargetPairWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    LastPairWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    LastDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetCuringTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActualLastCuringTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    TargetCycleTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActualLastCycleTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    LastFaultReason = table.Column<string>(type: "text", nullable: true),
                    LastNote = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionStations_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionStations_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionStations_ProductionSessions_ProductionSessionId",
                        column: x => x.ProductionSessionId,
                        principalTable: "ProductionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionStations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionDowntimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReasonType = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMinutes = table.Column<decimal>(type: "numeric", nullable: true),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionDowntimes_ProductionSessions_ProductionSessionId",
                        column: x => x.ProductionSessionId,
                        principalTable: "ProductionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionDowntimes_ProductionStations_ProductionStationId",
                        column: x => x.ProductionStationId,
                        principalTable: "ProductionStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CycleNumber = table.Column<long>(type: "bigint", nullable: true),
                    ProducedPairs = table.Column<long>(type: "bigint", nullable: true),
                    GoodPairs = table.Column<long>(type: "bigint", nullable: true),
                    FirePairs = table.Column<long>(type: "bigint", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Density = table.Column<decimal>(type: "numeric", nullable: true),
                    CuringTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    CycleTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorCode = table.Column<string>(type: "text", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_ProductionSessions_ProductionSessionId",
                        column: x => x.ProductionSessionId,
                        principalTable: "ProductionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_ProductionStations_ProductionStationId",
                        column: x => x.ProductionStationId,
                        principalTable: "ProductionStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionDowntimes_ProductionSessionId",
                table: "ProductionDowntimes",
                column: "ProductionSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionDowntimes_ProductionStationId",
                table: "ProductionDowntimes",
                column: "ProductionStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_ProductionSessionId",
                table: "ProductionEvents",
                column: "ProductionSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_ProductionStationId",
                table: "ProductionEvents",
                column: "ProductionStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_MachineId",
                table: "ProductionSessions",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_OperatorId",
                table: "ProductionSessions",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSessions_ProductId",
                table: "ProductionSessions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStations_MoldId",
                table: "ProductionStations",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStations_OperatorId",
                table: "ProductionStations",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStations_ProductId",
                table: "ProductionStations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStations_ProductionSessionId",
                table: "ProductionStations",
                column: "ProductionSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionDowntimes");

            migrationBuilder.DropTable(
                name: "ProductionEvents");

            migrationBuilder.DropTable(
                name: "ProductionStations");

            migrationBuilder.DropTable(
                name: "ProductionSessions");
        }
    }
}
