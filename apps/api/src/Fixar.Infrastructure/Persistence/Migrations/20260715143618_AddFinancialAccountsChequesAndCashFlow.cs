using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAccountsChequesAndCashFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerChequeId",
                table: "CustomerCollections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancePostingStatus",
                table: "CustomerCollections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancePostingWarning",
                table: "CustomerCollections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinancialAccountId",
                table: "CustomerCollections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinancialTransactionId",
                table: "CustomerCollections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerCheques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChequeNumber = table.Column<string>(type: "text", nullable: false),
                    PortfolioNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BankName = table.Column<string>(type: "text", nullable: false),
                    BankBranch = table.Column<string>(type: "text", nullable: true),
                    AccountHolder = table.Column<string>(type: "text", nullable: false),
                    DrawerName = table.Column<string>(type: "text", nullable: false),
                    ChequeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepositedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CollectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BouncedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    BankReference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_CustomerCheques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerCheques_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    BranchName = table.Column<string>(type: "text", nullable: true),
                    Iban = table.Column<string>(type: "text", nullable: true),
                    AccountNumber = table.Column<string>(type: "text", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningBalanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChequeEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerChequeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousStatus = table.Column<string>(type: "text", nullable: true),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChequeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChequeEvents_CustomerCheques_CustomerChequeId",
                        column: x => x.CustomerChequeId,
                        principalTable: "CustomerCheques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "text", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerCollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChequeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    BankReference = table.Column<string>(type: "text", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedBy = table.Column<string>(type: "text", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChequeEvents_CustomerChequeId_EventDate",
                table: "ChequeEvents",
                columns: new[] { "CustomerChequeId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCheques_CustomerCollectionId",
                table: "CustomerCheques",
                column: "CustomerCollectionId",
                unique: true,
                filter: "\"CustomerCollectionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCheques_CustomerId_Currency_Status_DueDate",
                table: "CustomerCheques",
                columns: new[] { "CustomerId", "Currency", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCheques_PortfolioNumber",
                table: "CustomerCheques",
                column: "PortfolioNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AccountCode",
                table: "FinancialAccounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AccountType_Currency_IsActive",
                table: "FinancialAccounts",
                columns: new[] { "AccountType", "Currency", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_Iban",
                table: "FinancialAccounts",
                column: "Iban",
                unique: true,
                filter: "\"Iban\" IS NOT NULL AND \"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_FinancialAccountId_Currency_Transacti~",
                table: "FinancialTransactions",
                columns: new[] { "FinancialAccountId", "Currency", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_SourceType_SourceId_Direction",
                table: "FinancialTransactions",
                columns: new[] { "SourceType", "SourceId", "Direction" },
                unique: true,
                filter: "\"SourceId\" IS NOT NULL AND \"IsReversed\" = FALSE AND \"SourceType\" <> 'AccountTransfer'");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_TransactionNumber",
                table: "FinancialTransactions",
                column: "TransactionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChequeEvents");

            migrationBuilder.DropTable(
                name: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "CustomerCheques");

            migrationBuilder.DropTable(
                name: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "CustomerChequeId",
                table: "CustomerCollections");

            migrationBuilder.DropColumn(
                name: "FinancePostingStatus",
                table: "CustomerCollections");

            migrationBuilder.DropColumn(
                name: "FinancePostingWarning",
                table: "CustomerCollections");

            migrationBuilder.DropColumn(
                name: "FinancialAccountId",
                table: "CustomerCollections");

            migrationBuilder.DropColumn(
                name: "FinancialTransactionId",
                table: "CustomerCollections");
        }
    }
}
