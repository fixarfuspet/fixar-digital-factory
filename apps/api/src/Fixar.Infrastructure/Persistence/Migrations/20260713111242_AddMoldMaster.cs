using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CadFilePath",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CavityCount",
                table: "Molds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CompatibleMachineCode",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentStationNumber",
                table: "Molds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EstimatedLifeCycles",
                table: "Molds",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FoamType",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRightLeftCombined",
                table: "Molds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCleaningDate",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumDensity",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumPairWeight",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumDensity",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPairWeight",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelCode",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoldType",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MoldWeightKg",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextCleaningDate",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextMaintenanceDate",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerCustomerName",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerType",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Molds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductModel",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleaseFrequencyCycles",
                table: "Molds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShelfCode",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Molds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SizeGroup",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandardCuringTimeSeconds",
                table: "Molds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandardCycleTimeSeconds",
                table: "Molds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardMoldTemperature",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageLocation",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetDensity",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetPairWeight",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalDocumentPath",
                table: "Molds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalCycleCount",
                table: "Molds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalProducedPairs",
                table: "Molds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Molds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "XCoordinate",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YCoordinate",
                table: "Molds",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Molds_ProductId",
                table: "Molds",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Molds_Products_ProductId",
                table: "Molds",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Molds_Products_ProductId",
                table: "Molds");

            migrationBuilder.DropIndex(
                name: "IX_Molds_ProductId",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CadFilePath",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CavityCount",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CompatibleMachineCode",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CurrentStationNumber",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "EstimatedLifeCycles",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "FoamType",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "IsRightLeftCombined",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "LastCleaningDate",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MaximumDensity",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MaximumPairWeight",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MinimumDensity",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MinimumPairWeight",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ModelCode",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MoldType",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "MoldWeightKg",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "NextCleaningDate",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "NextMaintenanceDate",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "OwnerCustomerName",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ProductModel",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ReleaseFrequencyCycles",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "ShelfCode",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "SizeGroup",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "StandardCuringTimeSeconds",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "StandardCycleTimeSeconds",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "StandardMoldTemperature",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "StorageLocation",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "TargetDensity",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "TargetPairWeight",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "TechnicalDocumentPath",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "TotalCycleCount",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "TotalProducedPairs",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "XCoordinate",
                table: "Molds");

            migrationBuilder.DropColumn(
                name: "YCoordinate",
                table: "Molds");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Molds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
