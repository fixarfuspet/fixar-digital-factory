using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandStockItemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "StockItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "StockItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "StockItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationCode",
                table: "StockItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "StockItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumQuantity",
                table: "StockItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumQuantity",
                table: "StockItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecipeUsageAmount",
                table: "StockItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyInfo",
                table: "StockItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "StockItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "StockItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseName",
                table: "StockItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WasteRate",
                table: "StockItems",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "LocationCode",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "MaximumQuantity",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "MinimumQuantity",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "RecipeUsageAmount",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "SafetyInfo",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "WarehouseName",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "WasteRate",
                table: "StockItems");
        }
    }
}
