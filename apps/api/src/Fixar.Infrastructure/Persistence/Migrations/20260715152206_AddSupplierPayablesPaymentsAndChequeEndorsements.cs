using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPayablesPaymentsAndChequeEndorsements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndorsedAt",
                table: "CustomerCheques",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndorsedBy",
                table: "CustomerCheques",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndorsedSupplierId",
                table: "CustomerCheques",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndorsementNotes",
                table: "CustomerCheques",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndorsementReference",
                table: "CustomerCheques",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierPaymentId",
                table: "CustomerCheques",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChequeEndorsements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsementNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerChequeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedBy = table.Column<string>(type: "text", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChequeEndorsements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChequeEndorsements_CustomerCheques_CustomerChequeId",
                        column: x => x.CustomerChequeId,
                        principalTable: "CustomerCheques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChequeEndorsements_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntryType = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierLedgerEntries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayableNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseOrderNumberSnapshot = table.Column<string>(type: "text", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPayables_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerChequeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    BankReference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UnallocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FinancePostingStatus = table.Column<string>(type: "text", nullable: false),
                    FinancePostingWarning = table.Column<string>(type: "text", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedBy = table.Column<string>(type: "text", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierPayableId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentAllocations_SupplierPayables_SupplierPayable~",
                        column: x => x.SupplierPayableId,
                        principalTable: "SupplierPayables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPaymentAllocations_SupplierPayments_SupplierPayment~",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChequeEndorsements_CustomerChequeId",
                table: "ChequeEndorsements",
                column: "CustomerChequeId",
                unique: true,
                filter: "\"Status\"='Active'");

            migrationBuilder.CreateIndex(
                name: "IX_ChequeEndorsements_EndorsementNumber",
                table: "ChequeEndorsements",
                column: "EndorsementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChequeEndorsements_SupplierId",
                table: "ChequeEndorsements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_EntryNumber",
                table: "SupplierLedgerEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_SourceType_SourceId_EntryType",
                table: "SupplierLedgerEntries",
                columns: new[] { "SourceType", "SourceId", "EntryType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_SupplierId_Currency_TransactionDate",
                table: "SupplierLedgerEntries",
                columns: new[] { "SupplierId", "Currency", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayables_PayableNumber",
                table: "SupplierPayables",
                column: "PayableNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayables_PurchaseOrderId",
                table: "SupplierPayables",
                column: "PurchaseOrderId",
                unique: true,
                filter: "\"PurchaseOrderId\" IS NOT NULL AND \"IsCancelled\"=FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayables_SupplierId_Currency_Status_DueDate",
                table: "SupplierPayables",
                columns: new[] { "SupplierId", "Currency", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentAllocations_SupplierPayableId",
                table: "SupplierPaymentAllocations",
                column: "SupplierPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPaymentAllocations_SupplierPaymentId_SupplierPayabl~",
                table: "SupplierPaymentAllocations",
                columns: new[] { "SupplierPaymentId", "SupplierPayableId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaymentNumber",
                table: "SupplierPayments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierId_Currency_Status_PaymentDate",
                table: "SupplierPayments",
                columns: new[] { "SupplierId", "Currency", "Status", "PaymentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChequeEndorsements");

            migrationBuilder.DropTable(
                name: "SupplierLedgerEntries");

            migrationBuilder.DropTable(
                name: "SupplierPaymentAllocations");

            migrationBuilder.DropTable(
                name: "SupplierPayables");

            migrationBuilder.DropTable(
                name: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "EndorsedAt",
                table: "CustomerCheques");

            migrationBuilder.DropColumn(
                name: "EndorsedBy",
                table: "CustomerCheques");

            migrationBuilder.DropColumn(
                name: "EndorsedSupplierId",
                table: "CustomerCheques");

            migrationBuilder.DropColumn(
                name: "EndorsementNotes",
                table: "CustomerCheques");

            migrationBuilder.DropColumn(
                name: "EndorsementReference",
                table: "CustomerCheques");

            migrationBuilder.DropColumn(
                name: "SupplierPaymentId",
                table: "CustomerCheques");
        }
    }
}
