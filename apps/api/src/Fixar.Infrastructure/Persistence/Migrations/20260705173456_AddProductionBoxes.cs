using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InjectionStations_Molds_CurrentMoldId",
                table: "InjectionStations");

            migrationBuilder.DropForeignKey(
                name: "FK_InjectionStations_Orders_CurrentOrderId",
                table: "InjectionStations");

            migrationBuilder.DropIndex(
                name: "IX_InjectionStations_CurrentMoldId",
                table: "InjectionStations");

            migrationBuilder.DropIndex(
                name: "IX_InjectionStations_CurrentOrderId",
                table: "InjectionStations");

            migrationBuilder.DropColumn(
                name: "CurrentMoldId",
                table: "InjectionStations");

            migrationBuilder.DropColumn(
                name: "CurrentOrderId",
                table: "InjectionStations");

            migrationBuilder.DropColumn(
                name: "LastMoldChangeAt",
                table: "InjectionStations");

            migrationBuilder.DropColumn(
                name: "ProductionStartedAt",
                table: "InjectionStations");

            migrationBuilder.CreateTable(
                name: "ProductionBoxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxCode = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    ProductionType = table.Column<string>(type: "text", nullable: true),
                    FabricColor = table.Column<string>(type: "text", nullable: true),
                    QuantityPairs = table.Column<int>(type: "integer", nullable: false),
                    CurrentStatus = table.Column<string>(type: "text", nullable: false),
                    CurrentLocation = table.Column<string>(type: "text", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    FilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionBoxes_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionBoxes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductionBoxes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductionBoxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionBoxId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    FromLocation = table.Column<string>(type: "text", nullable: true),
                    ToLocation = table.Column<string>(type: "text", nullable: true),
                    OperatorName = table.Column<string>(type: "text", nullable: true),
                    QuantityPairs = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBoxEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionBoxEvents_ProductionBoxes_ProductionBoxId",
                        column: x => x.ProductionBoxId,
                        principalTable: "ProductionBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxEvents_ProductionBoxId",
                table: "ProductionBoxEvents",
                column: "ProductionBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_MoldId",
                table: "ProductionBoxes",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_OrderId",
                table: "ProductionBoxes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBoxes_ProductId",
                table: "ProductionBoxes",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionBoxEvents");

            migrationBuilder.DropTable(
                name: "ProductionBoxes");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentMoldId",
                table: "InjectionStations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentOrderId",
                table: "InjectionStations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMoldChangeAt",
                table: "InjectionStations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProductionStartedAt",
                table: "InjectionStations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InjectionStations_CurrentMoldId",
                table: "InjectionStations",
                column: "CurrentMoldId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectionStations_CurrentOrderId",
                table: "InjectionStations",
                column: "CurrentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_InjectionStations_Molds_CurrentMoldId",
                table: "InjectionStations",
                column: "CurrentMoldId",
                principalTable: "Molds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InjectionStations_Orders_CurrentOrderId",
                table: "InjectionStations",
                column: "CurrentOrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
