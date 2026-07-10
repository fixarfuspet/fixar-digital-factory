using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkMaterialAndStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "StockItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_MaterialId",
                table: "StockItems",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Materials_MaterialId",
                table: "StockItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Materials_MaterialId",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_MaterialId",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "StockItems");
        }
    }
}
