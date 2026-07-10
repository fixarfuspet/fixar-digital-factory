using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    SubCategory = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaterialType = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    DefaultSupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSupplierName = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    LastPurchasePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric", nullable: true),
                    MaximumStock = table.Column<decimal>(type: "numeric", nullable: true),
                    CriticalStock = table.Column<decimal>(type: "numeric", nullable: true),
                    WarehouseName = table.Column<string>(type: "text", nullable: true),
                    LocationCode = table.Column<string>(type: "text", nullable: true),
                    LotTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiryTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TechnicalSpecification = table.Column<string>(type: "text", nullable: true),
                    SafetyInformation = table.Column<string>(type: "text", nullable: true),
                    ChemicalRole = table.Column<string>(type: "text", nullable: true),
                    Density = table.Column<decimal>(type: "numeric", nullable: true),
                    MixingRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    ContainerWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    AddedToPoliolBatch = table.Column<bool>(type: "boolean", nullable: false),
                    CrosskimApplicationNote = table.Column<string>(type: "text", nullable: true),
                    FabricType = table.Column<string>(type: "text", nullable: true),
                    FabricWeightGsm = table.Column<decimal>(type: "numeric", nullable: true),
                    FabricColor = table.Column<string>(type: "text", nullable: true),
                    FabricWidth = table.Column<decimal>(type: "numeric", nullable: true),
                    FabricRollLength = table.Column<decimal>(type: "numeric", nullable: true),
                    AdhesiveType = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    DtfCode = table.Column<string>(type: "text", nullable: true),
                    DtfName = table.Column<string>(type: "text", nullable: true),
                    DtfWidth = table.Column<decimal>(type: "numeric", nullable: true),
                    DtfHeight = table.Column<decimal>(type: "numeric", nullable: true),
                    ApplicationPosition = table.Column<string>(type: "text", nullable: true),
                    ApplicationNote = table.Column<string>(type: "text", nullable: true),
                    PackagingType = table.Column<string>(type: "text", nullable: true),
                    BoxPairCapacity = table.Column<int>(type: "integer", nullable: true),
                    BoxDimensions = table.Column<string>(type: "text", nullable: true),
                    BoxWeight = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Materials");
        }
    }
}
