using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireProductionBoxTraceabilityCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ProductionBoxes"
                SET "TraceabilityCode" = md5("Id"::text || ':fixar-traceability')
                WHERE "TraceabilityCode" IS NULL OR btrim("TraceabilityCode") = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TraceabilityCode",
                table: "ProductionBoxes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TraceabilityCode",
                table: "ProductionBoxes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
