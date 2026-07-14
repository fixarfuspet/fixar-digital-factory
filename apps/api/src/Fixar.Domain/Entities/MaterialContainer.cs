using Fixar.Domain.Common;
namespace Fixar.Domain.Entities;
public class MaterialContainer : BaseAuditableEntity
{
 public Guid MaterialLotId{get;set;} public MaterialLot MaterialLot{get;set;}=default!; public Guid MaterialId{get;set;} public Material Material{get;set;}=default!; public Guid StockItemId{get;set;} public StockItem StockItem{get;set;}=default!;
 public string ContainerCode{get;set;}=""; public string ContainerType{get;set;}="Drum"; public string? ManufacturerContainerNumber{get;set;} public decimal InitialQuantity{get;set;} public decimal CurrentQuantity{get;set;} public decimal ReservedQuantity{get;set;} public string Unit{get;set;}="kg";
 public DateTime? OpenedAt{get;set;} public string? OpenedBy{get;set;} public DateTime? ClosedAt{get;set;} public string? ClosedBy{get;set;} public string Status{get;set;}="Sealed"; public string? Warehouse{get;set;} public string? Location{get;set;} public string? RackCode{get;set;} public bool IsDamaged{get;set;} public string? DamageNotes{get;set;} public bool IsBlocked{get;set;} public string? BlockReason{get;set;} public string? Notes{get;set;} public bool IsActive{get;set;}=true; public DateTime CreatedAt{get;set;}=DateTime.UtcNow; public DateTime UpdatedAt{get;set;}=DateTime.UtcNow; public string? CreatedByName{get;set;} public string? UpdatedByName{get;set;}
}
