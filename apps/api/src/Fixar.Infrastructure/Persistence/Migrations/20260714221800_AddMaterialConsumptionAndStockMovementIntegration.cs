using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialConsumptionAndStockMovementIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConsumedQuantity",
                table: "StockReservationLines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConsumedAt",
                table: "StockReservationLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastConsumedByName",
                table: "StockReservationLines",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumptionNumber = table.Column<string>(type: "text", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReservationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumptionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    ConsumptionType = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    StockMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedByName = table.Column<string>(type: "text", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    ReversalStockMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_MaterialContainers_MaterialContainerId",
                        column: x => x.MaterialContainerId,
                        principalTable: "MaterialContainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_MaterialLots_MaterialLotId",
                        column: x => x.MaterialLotId,
                        principalTable: "MaterialLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_StationAssignments_StationAssignmentId",
                        column: x => x.StationAssignmentId,
                        principalTable: "StationAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_StockReservationLines_StockReservation~",
                        column: x => x.StockReservationLineId,
                        principalTable: "StockReservationLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_StockReservations_StockReservationId",
                        column: x => x.StockReservationId,
                        principalTable: "StockReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialConsumptions_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_ConsumptionDate",
                table: "MaterialConsumptions",
                column: "ConsumptionDate");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_ConsumptionNumber",
                table: "MaterialConsumptions",
                column: "ConsumptionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_ConsumptionType",
                table: "MaterialConsumptions",
                column: "ConsumptionType");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_IsReversed",
                table: "MaterialConsumptions",
                column: "IsReversed");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_MaterialContainerId",
                table: "MaterialConsumptions",
                column: "MaterialContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_MaterialId",
                table: "MaterialConsumptions",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_MaterialLotId",
                table: "MaterialConsumptions",
                column: "MaterialLotId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_StationAssignmentId",
                table: "MaterialConsumptions",
                column: "StationAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_StockItemId",
                table: "MaterialConsumptions",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_StockReservationId",
                table: "MaterialConsumptions",
                column: "StockReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_StockReservationLineId",
                table: "MaterialConsumptions",
                column: "StockReservationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialConsumptions_WorkOrderId",
                table: "MaterialConsumptions",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialConsumptions");

            migrationBuilder.DropColumn(
                name: "ConsumedQuantity",
                table: "StockReservationLines");

            migrationBuilder.DropColumn(
                name: "LastConsumedAt",
                table: "StockReservationLines");

            migrationBuilder.DropColumn(
                name: "LastConsumedByName",
                table: "StockReservationLines");
        }
    }
}
