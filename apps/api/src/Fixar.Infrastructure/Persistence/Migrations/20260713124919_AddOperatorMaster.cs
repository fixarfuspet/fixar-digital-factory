using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Operators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    NationalId = table.Column<string>(type: "text", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Shift = table.Column<int>(type: "integer", nullable: false),
                    DefaultMachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultMachineCode = table.Column<string>(type: "text", nullable: true),
                    DefaultMachineName = table.Column<string>(type: "text", nullable: true),
                    CanUseInjectionMachine = table.Column<bool>(type: "boolean", nullable: false),
                    CanUseGezerKafa = table.Column<bool>(type: "boolean", nullable: false),
                    CanUseDonerKafa = table.Column<bool>(type: "boolean", nullable: false),
                    CanUseDtfMachine = table.Column<bool>(type: "boolean", nullable: false),
                    CanPerformQualityControl = table.Column<bool>(type: "boolean", nullable: false),
                    CanPerformMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    CanApproveWorkOrder = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentMachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentMachineCode = table.Column<string>(type: "text", nullable: true),
                    CurrentMachineName = table.Column<string>(type: "text", nullable: true),
                    CurrentWorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentWorkOrderNumber = table.Column<string>(type: "text", nullable: true),
                    CurrentStationNumber = table.Column<int>(type: "integer", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: false),
                    TotalProducedPairs = table.Column<long>(type: "bigint", nullable: false),
                    TotalWorkingHours = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFirePairs = table.Column<long>(type: "bigint", nullable: false),
                    AverageFirePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    PerformancePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    QualityScore = table.Column<decimal>(type: "numeric", nullable: true),
                    LastPerformanceUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotoPath = table.Column<string>(type: "text", nullable: true),
                    QrCode = table.Column<string>(type: "text", nullable: true),
                    Barcode = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Operators_Machines_CurrentMachineId",
                        column: x => x.CurrentMachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Operators_Machines_DefaultMachineId",
                        column: x => x.DefaultMachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Operators_CurrentMachineId",
                table: "Operators",
                column: "CurrentMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_DefaultMachineId",
                table: "Operators",
                column: "DefaultMachineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Operators");
        }
    }
}
