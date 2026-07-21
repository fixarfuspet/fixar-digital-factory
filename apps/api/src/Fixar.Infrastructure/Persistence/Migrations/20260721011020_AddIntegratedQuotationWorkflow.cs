using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegratedQuotationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "integer", nullable: false),
                    PartialDeliveryAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TotalSalesAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedGrossProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedGrossMarginPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedLeadTimeDays = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CalculationWarnings = table.Column<string>(type: "text", nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quotes_Orders_ConvertedOrderId",
                        column: x => x.ConvertedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Size = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    FabricRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DtfRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LabelDescription = table.Column<string>(type: "text", nullable: true),
                    UnitEstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalEstimatedCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalSalesAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedGrossProfit = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedGrossMarginPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedLeadTimeDays = table.Column<decimal>(type: "numeric", nullable: true),
                    CalculationWarnings = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ProductId",
                table: "QuoteItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_QuoteId_LineNumber",
                table: "QuoteItems",
                columns: new[] { "QuoteId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ConvertedOrderId",
                table: "Quotes",
                column: "ConvertedOrderId",
                unique: true,
                filter: "\"ConvertedOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CustomerId_Status_QuoteDate",
                table: "Quotes",
                columns: new[] { "CustomerId", "Status", "QuoteDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteNumber",
                table: "Quotes",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ValidUntil",
                table: "Quotes",
                column: "ValidUntil");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteItems");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
