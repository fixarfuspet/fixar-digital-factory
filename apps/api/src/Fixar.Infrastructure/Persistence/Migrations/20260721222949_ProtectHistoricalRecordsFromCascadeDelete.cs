using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectHistoricalRecordsFromCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_CuttingMachines_CuttingMachineId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_Orders_OrderId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_InjectionStations_InjectionStationId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_Molds_MoldId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_Orders_OrderId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_StockItems_StockItemId",
                table: "PurchaseOrderLines");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_CuttingMachines_CuttingMachineId",
                table: "CuttingRecords",
                column: "CuttingMachineId",
                principalTable: "CuttingMachines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_Orders_OrderId",
                table: "CuttingRecords",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_InjectionStations_InjectionStationId",
                table: "ProductionRecords",
                column: "InjectionStationId",
                principalTable: "InjectionStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_Molds_MoldId",
                table: "ProductionRecords",
                column: "MoldId",
                principalTable: "Molds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_Orders_OrderId",
                table: "ProductionRecords",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_StockItems_StockItemId",
                table: "PurchaseOrderLines",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_CuttingMachines_CuttingMachineId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CuttingRecords_Orders_OrderId",
                table: "CuttingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_InjectionStations_InjectionStationId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_Molds_MoldId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionRecords_Orders_OrderId",
                table: "ProductionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_StockItems_StockItemId",
                table: "PurchaseOrderLines");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_CuttingMachines_CuttingMachineId",
                table: "CuttingRecords",
                column: "CuttingMachineId",
                principalTable: "CuttingMachines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingRecords_Orders_OrderId",
                table: "CuttingRecords",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Products_ProductId",
                table: "Orders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_InjectionStations_InjectionStationId",
                table: "ProductionRecords",
                column: "InjectionStationId",
                principalTable: "InjectionStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_Molds_MoldId",
                table: "ProductionRecords",
                column: "MoldId",
                principalTable: "Molds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionRecords_Orders_OrderId",
                table: "ProductionRecords",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_StockItems_StockItemId",
                table: "PurchaseOrderLines",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
