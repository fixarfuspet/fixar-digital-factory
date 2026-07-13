using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MachineType = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    StationCount = table.Column<int>(type: "integer", nullable: true),
                    DefaultCycleTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    MaximumDailyCapacity = table.Column<int>(type: "integer", nullable: true),
                    WorkingHoursPerDay = table.Column<decimal>(type: "numeric", nullable: true),
                    EnergyConsumption = table.Column<decimal>(type: "numeric", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    CurrentStatus = table.Column<string>(type: "text", nullable: false),
                    CurrentWorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentOperatorName = table.Column<string>(type: "text", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCleaningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextCleaningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCalibrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextCalibrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRunningHours = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalProducedPairs = table.Column<long>(type: "bigint", nullable: false),
                    AvailabilityPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    PerformancePercent = table.Column<decimal>(type: "numeric", nullable: true),
                    QualityPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    OEE = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Machines");
        }
    }
}
