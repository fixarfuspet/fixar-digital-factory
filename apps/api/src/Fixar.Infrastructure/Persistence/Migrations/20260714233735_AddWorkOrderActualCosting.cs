using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderActualCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultEnergyCostPerKwh",
                table: "Machines",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyOperatingCost",
                table: "Machines",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PowerKw",
                table: "Machines",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CostSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportingCurrency = table.Column<string>(type: "text", nullable: false),
                    DefaultHourlyLaborCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultEnergyCostPerKwh = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultMachineCostPerHour = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultOverheadRatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultFireCostMethod = table.Column<string>(type: "text", nullable: false),
                    DefaultCuttingCostPerPair = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultPackagingCostPerPair = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultQualityCostPerPair = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultShipmentPreparationCostPerBox = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BaseCurrency = table.Column<string>(type: "text", nullable: false),
                    QuoteCurrency = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderCostSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotNumber = table.Column<string>(type: "text", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalculationType = table.Column<string>(type: "text", nullable: false),
                    ReportingCurrency = table.Column<string>(type: "text", nullable: false),
                    PlannedPairs = table.Column<int>(type: "integer", nullable: false),
                    ProducedPairs = table.Column<int>(type: "integer", nullable: false),
                    GoodPairs = table.Column<int>(type: "integer", nullable: false),
                    FirePairs = table.Column<int>(type: "integer", nullable: false),
                    CutPairs = table.Column<int>(type: "integer", nullable: false),
                    PackedPairs = table.Column<int>(type: "integer", nullable: false),
                    EstimatedMaterialCost = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualMaterialCost = table.Column<decimal>(type: "numeric", nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric", nullable: false),
                    EnergyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    MachineCost = table.Column<decimal>(type: "numeric", nullable: false),
                    FireCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CuttingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PackagingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    QualityCost = table.Column<decimal>(type: "numeric", nullable: false),
                    OverheadCost = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalActualCost = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedCostPerPair = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualCostPerProducedPair = table.Column<decimal>(type: "numeric", nullable: true),
                    ActualCostPerGoodPair = table.Column<decimal>(type: "numeric", nullable: true),
                    VarianceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    VariancePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    SalesRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossProfit = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossMarginPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderCostSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderCostSnapshots_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderCostLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderCostSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCategory = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: false),
                    SourceCurrency = table.Column<string>(type: "text", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ReportingCurrency = table.Column<string>(type: "text", nullable: false),
                    TotalSourceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalReportingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderCostLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderCostLines_WorkOrderCostSnapshots_WorkOrderCostSnap~",
                        column: x => x.WorkOrderCostSnapshotId,
                        principalTable: "WorkOrderCostSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostSettings_IsActive_EffectiveFrom_EffectiveTo",
                table: "CostSettings",
                columns: new[] { "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_IsActive",
                table: "ExchangeRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_RateDate_BaseCurrency_QuoteCurrency",
                table: "ExchangeRates",
                columns: new[] { "RateDate", "BaseCurrency", "QuoteCurrency" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCostLines_WorkOrderCostSnapshotId_CostCategory",
                table: "WorkOrderCostLines",
                columns: new[] { "WorkOrderCostSnapshotId", "CostCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCostSnapshots_IsFinal_CalculationType_ReportingCur~",
                table: "WorkOrderCostSnapshots",
                columns: new[] { "IsFinal", "CalculationType", "ReportingCurrency" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCostSnapshots_SnapshotNumber",
                table: "WorkOrderCostSnapshots",
                column: "SnapshotNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCostSnapshots_WorkOrderId_SnapshotDate",
                table: "WorkOrderCostSnapshots",
                columns: new[] { "WorkOrderId", "SnapshotDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostSettings");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "WorkOrderCostLines");

            migrationBuilder.DropTable(
                name: "WorkOrderCostSnapshots");

            migrationBuilder.DropColumn(
                name: "DefaultEnergyCostPerKwh",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "HourlyOperatingCost",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "PowerKw",
                table: "Machines");
        }
    }
}
