using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegratedFinanceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AffectsBalance",
                table: "FinancialTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessReference",
                table: "FinancialTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyName",
                table: "FinancialTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "FinancialTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "FinancialTransactions",
                type: "numeric",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<Guid>(
                name: "FinanceCategoryId",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "FinancialTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReportingAmount",
                table: "FinancialTransactions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReportingCurrency",
                table: "FinancialTransactions",
                type: "text",
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            // Existing transactions are historical source-of-truth movements. Preserve
            // their balance effect and create a deterministic 1:1 reporting snapshot.
            migrationBuilder.Sql("""
                UPDATE "FinancialTransactions"
                SET "AffectsBalance" = TRUE,
                    "ExchangeRate" = 1,
                    "ReportingCurrency" = "Currency",
                    "ReportingAmount" = "Amount"
                """);

            migrationBuilder.CreateTable(
                name: "AccountReconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationNumber = table.Column<string>(type: "text", nullable: false),
                    AccountPartyType = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    PeriodDebit = table.Column<decimal>(type: "numeric", nullable: false),
                    PeriodCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CounterpartyNote = table.Column<string>(type: "text", nullable: true),
                    InternalNote = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountReconciliations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountReconciliations_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CategoryType = table.Column<string>(type: "text", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCenter = table.Column<string>(type: "text", nullable: true),
                    IncludeInProductionCost = table.Column<bool>(type: "boolean", nullable: false),
                    CostBehavior = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceCategories_FinanceCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "FinanceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_BusinessReference",
                table: "FinancialTransactions",
                column: "BusinessReference",
                unique: true,
                filter: "\"BusinessReference\" IS NOT NULL AND \"IsReversed\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CustomerId_TransactionDate",
                table: "FinancialTransactions",
                columns: new[] { "CustomerId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_FinanceCategoryId_TransactionDate",
                table: "FinancialTransactions",
                columns: new[] { "FinanceCategoryId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_OrderId",
                table: "FinancialTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_PurchaseOrderId",
                table: "FinancialTransactions",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_SupplierId_TransactionDate",
                table: "FinancialTransactions",
                columns: new[] { "SupplierId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountReconciliations_AccountPartyType_CustomerId_Supplier~",
                table: "AccountReconciliations",
                columns: new[] { "AccountPartyType", "CustomerId", "SupplierId", "Currency", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountReconciliations_CustomerId",
                table: "AccountReconciliations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountReconciliations_ReconciliationNumber",
                table: "AccountReconciliations",
                column: "ReconciliationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountReconciliations_SupplierId",
                table: "AccountReconciliations",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_CategoryType_IsActive",
                table: "FinanceCategories",
                columns: new[] { "CategoryType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_Code",
                table: "FinanceCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_ParentCategoryId",
                table: "FinanceCategories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_FinanceCategories_FinanceCategoryId",
                table: "FinancialTransactions",
                column: "FinanceCategoryId",
                principalTable: "FinanceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_Orders_OrderId",
                table: "FinancialTransactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_PurchaseOrders_PurchaseOrderId",
                table: "FinancialTransactions",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_Suppliers_SupplierId",
                table: "FinancialTransactions",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_FinanceCategories_FinanceCategoryId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_Orders_OrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_PurchaseOrders_PurchaseOrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_Suppliers_SupplierId",
                table: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "AccountReconciliations");

            migrationBuilder.DropTable(
                name: "FinanceCategories");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_BusinessReference",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_CustomerId_TransactionDate",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_FinanceCategoryId_TransactionDate",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_OrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_PurchaseOrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_SupplierId_TransactionDate",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "AffectsBalance",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "BusinessReference",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "CounterpartyName",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "FinanceCategoryId",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "ReportingAmount",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "ReportingCurrency",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "FinancialTransactions");
        }
    }
}
