using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCuttingBoxWarehouseShipmentFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoxNumber",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNameSnapshot",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CuttingRecordId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductionBoxes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "ProductionBoxes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackedAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackedByOperatorId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PairCount",
                table: "ProductionBoxes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductCodeSnapshot",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RackCode",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyForShipmentAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedToWarehouseAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedToWarehouseBy",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentNotes",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentReference",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StationAssignmentId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProductionBoxes",
                type: "text",
                nullable: false,
                defaultValue: "Packed");

            migrationBuilder.AddColumn<string>(
                name: "TraceabilityCode",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductionBoxes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseLocation",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderId",
                table: "ProductionBoxes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNumberSnapshot",
                table: "ProductionBoxes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "CuttingRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CuttingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoodPairs",
                table: "CuttingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InputPairs",
                table: "CuttingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CuttingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "CuttingRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                table: "CuttingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "CuttingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "CuttingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordDate",
                table: "CuttingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "RecordNumber",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedPairs",
                table: "CuttingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReworkPairs",
                table: "CuttingRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Shift",
                table: "CuttingRecords",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StationAssignmentId",
                table: "CuttingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CuttingRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "CuttingRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkOrderId",
                table: "CuttingRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ProductionBoxes"
                SET
                    "BoxNumber" = COALESCE("BoxNumber", NULLIF("BoxCode", '')),
                    "Barcode" = COALESCE("Barcode", NULLIF("BoxCode", '')),
                    "PairCount" = CASE WHEN "PairCount" = 0 THEN COALESCE("QuantityPairs", 0) ELSE "PairCount" END,
                    "CustomerNameSnapshot" = COALESCE("CustomerNameSnapshot", NULLIF("CustomerName", '')),
                    "WarehouseLocation" = COALESCE("WarehouseLocation", NULLIF("CurrentLocation", '')),
                    "PackedAt" = COALESCE("PackedAt", "FilledAt"),
                    "CreatedAt" = COALESCE("FilledAt", NOW()),
                    "UpdatedAt" = NOW(),
                    "IsActive" = true,
                    "Status" = CASE
                        WHEN "CurrentStatus" = 'Sevk Edildi' THEN 'Shipped'
                        WHEN "CurrentStatus" = 'Depoya Girdi' THEN 'InWarehouse'
                        WHEN "CurrentStatus" = 'Kesim Bitti' THEN 'ReadyForShipment'
                        WHEN "CurrentStatus" = 'Boş' THEN 'Packed'
                        WHEN "CurrentStatus" = 'Üretimden Çıktı' THEN 'Packed'
                        ELSE COALESCE(NULLIF("Status", ''), 'Packed')
                    END
                WHERE "IsCancelled" = false;
                """);

            migrationBuilder.Sql("""
                UPDATE "CuttingRecords"
                SET
                    "InputPairs" = CASE WHEN "InputPairs" = 0 THEN COALESCE("CutPairs", 0) ELSE "InputPairs" END,
                    "GoodPairs" = CASE WHEN "GoodPairs" = 0 THEN COALESCE("CutPairs", 0) ELSE "GoodPairs" END,
                    "RecordDate" = COALESCE("StartTime", NOW()),
                    "CreatedAt" = COALESCE("StartTime", NOW()),
                    "UpdatedAt" = COALESCE("EndTime", NOW()),
                    "IsActive" = true
                WHERE "IsCancelled" = false;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_Barcode",
                table: "ProductionBoxes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_BoxNumber",
                table: "ProductionBoxes",
                column: "BoxNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_CustomerId",
                table: "ProductionBoxes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_CuttingRecordId",
                table: "ProductionBoxes",
                column: "CuttingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_OrderItemId",
                table: "ProductionBoxes",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_PackedByOperatorId",
                table: "ProductionBoxes",
                column: "PackedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_StationAssignmentId",
                table: "ProductionBoxes",
                column: "StationAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_Status",
                table: "ProductionBoxes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_TraceabilityCode",
                table: "ProductionBoxes",
                column: "TraceabilityCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_WorkOrderId",
                table: "ProductionBoxes",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_OperatorId",
                table: "CuttingRecords",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_OrderItemId",
                table: "CuttingRecords",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_ProductId",
                table: "CuttingRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_RecordDate",
                table: "CuttingRecords",
                column: "RecordDate");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_RecordNumber",
                table: "CuttingRecords",
                column: "RecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_StationAssignmentId",
                table: "CuttingRecords",
                column: "StationAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_Status",
                table: "CuttingRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CuttingRecords_WorkOrderId",
                table: "CuttingRecords",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_Operators_OperatorId",
                table: "CuttingRecords",
                column: "OperatorId",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_OrderItems_OrderItemId",
                table: "CuttingRecords",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_Products_ProductId",
                table: "CuttingRecords",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_StationAssignments_StationAssignmentId",
                table: "CuttingRecords",
                column: "StationAssignmentId",
                principalTable: "StationAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_WorkOrders_WorkOrderId",
                table: "CuttingRecords",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_Customers_CustomerId",
                table: "ProductionBoxes",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_CuttingRecords_CuttingRecordId",
                table: "ProductionBoxes",
                column: "CuttingRecordId",
                principalTable: "CuttingRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_Operators_PackedByOperatorId",
                table: "ProductionBoxes",
                column: "PackedByOperatorId",
                principalTable: "Operators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_OrderItems_OrderItemId",
                table: "ProductionBoxes",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_StationAssignments_StationAssignmentId",
                table: "ProductionBoxes",
                column: "StationAssignmentId",
                principalTable: "StationAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionBoxes_WorkOrders_WorkOrderId",
                table: "ProductionBoxes",
                column: "WorkOrderId",
                principalTable: "WorkOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_Operators_OperatorId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_OrderItems_OrderItemId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_Products_ProductId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_StationAssignments_StationAssignmentId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_WorkOrders_WorkOrderId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_Customers_CustomerId",
                table: "ProductionBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_CuttingRecords_CuttingRecordId",
                table: "ProductionBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_Operators_PackedByOperatorId",
                table: "ProductionBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_OrderItems_OrderItemId",
                table: "ProductionBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_StationAssignments_StationAssignmentId",
                table: "ProductionBoxes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionBoxes_WorkOrders_WorkOrderId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_Barcode",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_BoxNumber",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_CustomerId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_CuttingRecordId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_OrderItemId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_PackedByOperatorId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_StationAssignmentId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_Status",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_TraceabilityCode",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_ProductionBoxes_WorkOrderId",
                table: "ProductionBoxes");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_OperatorId",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_OrderItemId",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_ProductId",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_RecordDate",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_RecordNumber",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_StationAssignmentId",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_Status",
                table: "CuttingRecords");

            migrationBuilder.DropIndex(
                name: "IX_CuttingRecords_WorkOrderId",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "BoxNumber",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CustomerNameSnapshot",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CuttingRecordId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "PackedAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "PackedByOperatorId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "PairCount",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ProductCodeSnapshot",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "RackCode",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ReadyForShipmentAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ReceivedToWarehouseAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ReceivedToWarehouseBy",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ShipmentNotes",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ShipmentReference",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "StationAssignmentId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "TraceabilityCode",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "WarehouseLocation",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "WorkOrderNumberSnapshot",
                table: "ProductionBoxes");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "GoodPairs",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "InputPairs",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "RecordDate",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "RecordNumber",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "RejectedPairs",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "ReworkPairs",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "StationAssignmentId",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "CuttingRecords");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "CuttingRecords");
        }
    }
}
