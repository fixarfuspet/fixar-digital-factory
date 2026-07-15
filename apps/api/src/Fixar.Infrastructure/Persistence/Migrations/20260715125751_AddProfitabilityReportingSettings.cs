using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitabilityReportingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfitabilitySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HighMarginThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LowMarginThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    BreakEvenTolerancePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    FireWarningThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CostVarianceWarningThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveryDelayWarningDays = table.Column<int>(type: "integer", nullable: false),
                    MinimumDataCompletenessPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitabilitySettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfitabilitySettings_IsActive_EffectiveFrom_EffectiveTo",
                table: "ProfitabilitySettings",
                columns: new[] { "IsActive", "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfitabilitySettings");
        }
    }
}
