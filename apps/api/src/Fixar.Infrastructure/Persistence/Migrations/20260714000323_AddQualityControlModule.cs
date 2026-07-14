using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityControlModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionNumber = table.Column<string>(type: "text", nullable: false),
                    InspectionType = table.Column<string>(type: "text", nullable: false),
                    StationAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Shift = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    SampleSizePairs = table.Column<int>(type: "integer", nullable: false),
                    CheckedPairs = table.Column<int>(type: "integer", nullable: false),
                    AcceptedPairs = table.Column<int>(type: "integer", nullable: false),
                    RejectedPairs = table.Column<int>(type: "integer", nullable: false),
                    ConditionalAcceptedPairs = table.Column<int>(type: "integer", nullable: false),
                    TargetWeightGrams = table.Column<decimal>(type: "numeric", nullable: true),
                    MeasuredWeightGrams = table.Column<decimal>(type: "numeric", nullable: true),
                    WeightToleranceMinus = table.Column<decimal>(type: "numeric", nullable: true),
                    WeightTolerancePlus = table.Column<decimal>(type: "numeric", nullable: true),
                    WeightResult = table.Column<string>(type: "text", nullable: false),
                    TargetDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    MeasuredDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    DensityMinimum = table.Column<decimal>(type: "numeric", nullable: true),
                    DensityMaximum = table.Column<decimal>(type: "numeric", nullable: true),
                    DensityResult = table.Column<string>(type: "text", nullable: false),
                    TargetX = table.Column<decimal>(type: "numeric", nullable: true),
                    MeasuredX = table.Column<decimal>(type: "numeric", nullable: true),
                    TargetY = table.Column<decimal>(type: "numeric", nullable: true),
                    MeasuredY = table.Column<decimal>(type: "numeric", nullable: true),
                    DimensionTolerance = table.Column<decimal>(type: "numeric", nullable: true),
                    DimensionResult = table.Column<string>(type: "text", nullable: false),
                    VisualResult = table.Column<string>(type: "text", nullable: false),
                    ColorResult = table.Column<string>(type: "text", nullable: false),
                    SurfaceResult = table.Column<string>(type: "text", nullable: false),
                    FabricBondingResult = table.Column<string>(type: "text", nullable: false),
                    GeneralNotes = table.Column<string>(type: "text", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "text", nullable: true),
                    HoldProduction = table.Column<bool>(type: "boolean", nullable: false),
                    CreateFireRecord = table.Column<bool>(type: "boolean", nullable: false),
                    FireReason = table.Column<string>(type: "text", nullable: true),
                    FirePairs = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    UpdatedByName = table.Column<string>(type: "text", nullable: true),
                    ProductCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    CustomerNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    WorkOrderNumberSnapshot = table.Column<string>(type: "text", nullable: true),
                    MoldCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    OperatorNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Molds_MoldId",
                        column: x => x.MoldId,
                        principalTable: "Molds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_StationAssignments_StationAssignmentId",
                        column: x => x.StationAssignmentId,
                        principalTable: "StationAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityDefects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityInspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefectType = table.Column<string>(type: "text", nullable: false),
                    DefectCode = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefectPairs = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IsFireRelated = table.Column<bool>(type: "boolean", nullable: false),
                    StationAssignmentFireId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "text", nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityDefects_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityDefects_StationAssignmentFires_StationAssignmentFire~",
                        column: x => x.StationAssignmentFireId,
                        principalTable: "StationAssignmentFires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefects_DefectType",
                table: "QualityDefects",
                column: "DefectType");

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefects_QualityInspectionId",
                table: "QualityDefects",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefects_StationAssignmentFireId",
                table: "QualityDefects",
                column: "StationAssignmentFireId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionDate",
                table: "QualityInspections",
                column: "InspectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionNumber",
                table: "QualityInspections",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_InspectionType",
                table: "QualityInspections",
                column: "InspectionType");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_IsActive",
                table: "QualityInspections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_MachineId",
                table: "QualityInspections",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_MoldId",
                table: "QualityInspections",
                column: "MoldId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_OperatorId",
                table: "QualityInspections",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_OrderItemId",
                table: "QualityInspections",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductId",
                table: "QualityInspections",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_Result",
                table: "QualityInspections",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_StationAssignmentId",
                table: "QualityInspections",
                column: "StationAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_WorkOrderId",
                table: "QualityInspections",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityDefects");

            migrationBuilder.DropTable(
                name: "QualityInspections");
        }
    }
}
