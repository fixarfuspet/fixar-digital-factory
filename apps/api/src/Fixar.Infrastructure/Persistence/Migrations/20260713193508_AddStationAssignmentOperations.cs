using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStationAssignmentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirePairs",
                table: "StationAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReleaseAt",
                table: "StationAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReleaseTurn",
                table: "StationAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTurnAt",
                table: "StationAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleaseFrequencyTurns",
                table: "StationAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTurns",
                table: "StationAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TurnsSinceLastRelease",
                table: "StationAssignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StationAssignmentDowntimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationNumberSnapshot = table.Column<int>(type: "integer", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    DowntimeType = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    PreviousAssignmentStatus = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMinutes = table.Column<decimal>(type: "numeric", nullable: true),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    StartedBy = table.Column<string>(type: "text", nullable: true),
                    EndedBy = table.Column<string>(type: "text", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationAssignmentDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationAssignmentDowntimes_StationAssignments_StationAssign~",
                        column: x => x.StationAssignmentId,
                        principalTable: "StationAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StationAssignmentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationNumberSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    MoldNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    EventTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RecordedBy = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationAssignmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationAssignmentEvents_StationAssignments_StationAssignmen~",
                        column: x => x.StationAssignmentId,
                        principalTable: "StationAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StationAssignmentFires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StationAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InjectionStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationNumberSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    MoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoldCodeSnapshot = table.Column<string>(type: "text", nullable: true),
                    MoldNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperatorNameSnapshot = table.Column<string>(type: "text", nullable: true),
                    FirePairs = table.Column<int>(type: "integer", nullable: false),
                    ReasonType = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<string>(type: "text", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationAssignmentFires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationAssignmentFires_StationAssignments_StationAssignment~",
                        column: x => x.StationAssignmentId,
                        principalTable: "StationAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentDowntimes_IsOpen",
                table: "StationAssignmentDowntimes",
                column: "IsOpen");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentDowntimes_StationAssignmentId",
                table: "StationAssignmentDowntimes",
                column: "StationAssignmentId",
                unique: true,
                filter: "\"IsOpen\" = true AND \"IsCancelled\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentEvents_EventTime",
                table: "StationAssignmentEvents",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentEvents_EventType",
                table: "StationAssignmentEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentEvents_StationAssignmentId",
                table: "StationAssignmentEvents",
                column: "StationAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentFires_RecordedAt",
                table: "StationAssignmentFires",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StationAssignmentFires_StationAssignmentId",
                table: "StationAssignmentFires",
                column: "StationAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationAssignmentDowntimes");

            migrationBuilder.DropTable(
                name: "StationAssignmentEvents");

            migrationBuilder.DropTable(
                name: "StationAssignmentFires");

            migrationBuilder.DropColumn(
                name: "FirePairs",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "LastReleaseAt",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "LastReleaseTurn",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "LastTurnAt",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "ReleaseFrequencyTurns",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "TotalTurns",
                table: "StationAssignments");

            migrationBuilder.DropColumn(
                name: "TurnsSinceLastRelease",
                table: "StationAssignments");
        }
    }
}
