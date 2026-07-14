using Fixar.Domain.Common;
namespace Fixar.Domain.Entities;
public class MaterialLot : BaseAuditableEntity
{
 public Guid MaterialId{get;set;} public Material Material{get;set;}=default!; public Guid StockItemId{get;set;} public StockItem StockItem{get;set;}=default!;
 public Guid? SupplierId{get;set;} public Supplier? Supplier{get;set;} public Guid? PurchaseOrderId{get;set;} public PurchaseOrder? PurchaseOrder{get;set;} public Guid? PurchaseOrderLineId{get;set;} public PurchaseOrderLine? PurchaseOrderLine{get;set;}
 public string LotNumber{get;set;}=""; public string? SupplierLotNumber{get;set;} public string? BatchNumber{get;set;} public DateTime ReceivedDate{get;set;}=DateTime.UtcNow; public DateTime? ProductionDate{get;set;} public DateTime? ExpiryDate{get;set;}
 public decimal InitialQuantity{get;set;} public decimal CurrentQuantity{get;set;} public decimal ReservedQuantity{get;set;} public string Unit{get;set;}="kg"; public decimal? UnitPrice{get;set;} public string Currency{get;set;}="EUR";
 public string? Warehouse{get;set;} public string? Location{get;set;} public string? RackCode{get;set;} public string Status{get;set;}="Available"; public string QualityStatus{get;set;}="Pending"; public bool IsBlocked{get;set;} public string? BlockReason{get;set;} public string? Notes{get;set;} public bool IsActive{get;set;}=true; public DateTime CreatedAt{get;set;}=DateTime.UtcNow; public DateTime UpdatedAt{get;set;}=DateTime.UtcNow; public string? CreatedByName{get;set;} public string? UpdatedByName{get;set;} public ICollection<MaterialContainer> Containers{get;set;}=[];
}
