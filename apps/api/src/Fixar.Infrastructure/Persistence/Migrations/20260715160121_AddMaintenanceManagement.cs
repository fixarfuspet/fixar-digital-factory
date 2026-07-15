using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetCode = table.Column<string>(type: "text", nullable: false),
                    AssetName = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CuttingMachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    CommissioningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Criticality = table.Column<string>(type: "text", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceStrategy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceAssets_CuttingMachines_CuttingMachineId",
                        column: x => x.CuttingMachineId,
                        principalTable: "CuttingMachines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceAssets_InjectionStations_InjectionStationId",
                        column: x => x.InjectionStationId,
                        principalTable: "InjectionStations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceAssets_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceAssets_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<string>(type: "text", nullable: true),
                    WorkType = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "text", nullable: false),
                    MaintenanceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByName = table.Column<string>(type: "text", nullable: true),
                    ProductionImpact = table.Column<string>(type: "text", nullable: false),
                    MachineStopped = table.Column<bool>(type: "boolean", nullable: false),
                    DowntimeStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedDowntimeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToName = table.Column<string>(type: "text", nullable: true),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionSummary = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_MaintenanceAssets_MaintenanceAssetId",
                        column: x => x.MaintenanceAssetId,
                        principalTable: "MaintenanceAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_ProductionDowntimes_RelatedDowntimeId",
                        column: x => x.RelatedDowntimeId,
                        principalTable: "ProductionDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ItemText = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedValue = table.Column<string>(type: "text", nullable: true),
                    MinimumValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaximumValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistTemplateItems_MaintenanceChecklistTempl~",
                        column: x => x.MaintenanceChecklistTemplateId,
                        principalTable: "MaintenanceChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreventiveMaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanCode = table.Column<string>(type: "text", nullable: false),
                    MaintenanceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FrequencyType = table.Column<string>(type: "text", nullable: false),
                    FrequencyValue = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastGeneratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequiresProductionStop = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultPriority = table.Column<string>(type: "text", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AutoCreateWorkOrder = table.Column<bool>(type: "boolean", nullable: false),
                    AdvanceCreateDays = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenancePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenancePlans_MaintenanceAssets_MaintenanceAss~",
                        column: x => x.MaintenanceAssetId,
                        principalTable: "MaintenanceAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenancePlans_MaintenanceChecklistTemplates_Ch~",
                        column: x => x.ChecklistTemplateId,
                        principalTable: "MaintenanceChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceWorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceWorkOrderNumber = table.Column<string>(type: "text", nullable: false),
                    MaintenanceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreventiveMaintenancePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkType = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PlannedStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToName = table.Column<string>(type: "text", nullable: true),
                    ExternalServiceSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiresProductionStop = table.Column<bool>(type: "boolean", nullable: false),
                    DowntimeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DowntimeMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    LaborMinutes = table.Column<decimal>(type: "numeric", nullable: false),
                    FailureCause = table.Column<string>(type: "text", nullable: true),
                    WorkPerformed = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    VerificationNotes = table.Column<string>(type: "text", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalPartsCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalLaborCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalExternalServiceCost = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalMaintenanceCost = table.Column<decimal>(type: "numeric", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceWorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceWorkOrders_MaintenanceAssets_MaintenanceAssetId",
                        column: x => x.MaintenanceAssetId,
                        principalTable: "MaintenanceAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceWorkOrders_MaintenanceRequests_MaintenanceReques~",
                        column: x => x.MaintenanceRequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceWorkOrders_ProductionDowntimes_DowntimeId",
                        column: x => x.DowntimeId,
                        principalTable: "ProductionDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceWorkOrders_Suppliers_ExternalServiceSupplierId",
                        column: x => x.ExternalServiceSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceChecklistResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    PassFail = table.Column<bool>(type: "boolean", nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedByName = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceChecklistResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistResults_MaintenanceChecklistTemplateIte~",
                        column: x => x.TemplateItemId,
                        principalTable: "MaintenanceChecklistTemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceChecklistResults_MaintenanceWorkOrders_Maintenan~",
                        column: x => x.MaintenanceWorkOrderId,
                        principalTable: "MaintenanceWorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenancePartUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false),
                    ReversalStockMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenancePartUsages_MaintenanceWorkOrders_MaintenanceWork~",
                        column: x => x.MaintenanceWorkOrderId,
                        principalTable: "MaintenanceWorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenancePartUsages_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenancePartUsages_StockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "StockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAssets_AssetCode",
                table: "MaintenanceAssets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAssets_CuttingMachineId",
                table: "MaintenanceAssets",
                column: "CuttingMachineId",
                unique: true,
                filter: "\"CuttingMachineId\" IS NOT NULL AND \"IsActive\"=TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAssets_InjectionStationId",
                table: "MaintenanceAssets",
                column: "InjectionStationId",
                unique: true,
                filter: "\"InjectionStationId\" IS NOT NULL AND \"IsActive\"=TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAssets_MachineId",
                table: "MaintenanceAssets",
                column: "MachineId",
                unique: true,
                filter: "\"MachineId\" IS NOT NULL AND \"IsActive\"=TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAssets_MoldId",
                table: "MaintenanceAssets",
                column: "MoldId",
                unique: true,
                filter: "\"MoldId\" IS NOT NULL AND \"IsActive\"=TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistResults_MaintenanceWorkOrderId_Template~",
                table: "MaintenanceChecklistResults",
                columns: new[] { "MaintenanceWorkOrderId", "TemplateItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistResults_TemplateItemId",
                table: "MaintenanceChecklistResults",
                column: "TemplateItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceChecklistTemplateItems_MaintenanceChecklistTempl~",
                table: "MaintenanceChecklistTemplateItems",
                columns: new[] { "MaintenanceChecklistTemplateId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePartUsages_MaintenanceWorkOrderId_Status",
                table: "MaintenancePartUsages",
                columns: new[] { "MaintenanceWorkOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePartUsages_MaterialId",
                table: "MaintenancePartUsages",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePartUsages_StockItemId",
                table: "MaintenancePartUsages",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_MaintenanceAssetId_Status",
                table: "MaintenanceRequests",
                columns: new[] { "MaintenanceAssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_RelatedDowntimeId",
                table: "MaintenanceRequests",
                column: "RelatedDowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_RequestNumber",
                table: "MaintenanceRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_DowntimeId",
                table: "MaintenanceWorkOrders",
                column: "DowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_ExternalServiceSupplierId",
                table: "MaintenanceWorkOrders",
                column: "ExternalServiceSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_MaintenanceAssetId",
                table: "MaintenanceWorkOrders",
                column: "MaintenanceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_MaintenanceRequestId_Status",
                table: "MaintenanceWorkOrders",
                columns: new[] { "MaintenanceRequestId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_MaintenanceWorkOrderNumber",
                table: "MaintenanceWorkOrders",
                column: "MaintenanceWorkOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWorkOrders_PreventiveMaintenancePlanId_PlannedSt~",
                table: "MaintenanceWorkOrders",
                columns: new[] { "PreventiveMaintenancePlanId", "PlannedStart" },
                unique: true,
                filter: "\"PreventiveMaintenancePlanId\" IS NOT NULL AND \"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_ChecklistTemplateId",
                table: "PreventiveMaintenancePlans",
                column: "ChecklistTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_IsActive_NextDueDate",
                table: "PreventiveMaintenancePlans",
                columns: new[] { "IsActive", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_MaintenanceAssetId",
                table: "PreventiveMaintenancePlans",
                column: "MaintenanceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_PlanCode",
                table: "PreventiveMaintenancePlans",
                column: "PlanCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceChecklistResults");

            migrationBuilder.DropTable(
                name: "MaintenancePartUsages");

            migrationBuilder.DropTable(
                name: "PreventiveMaintenancePlans");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistTemplateItems");

            migrationBuilder.DropTable(
                name: "MaintenanceWorkOrders");

            migrationBuilder.DropTable(
                name: "MaintenanceChecklistTemplates");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "MaintenanceAssets");
        }
    }
}
