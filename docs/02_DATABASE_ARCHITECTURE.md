# FIXAR OS

# Database Architecture

Version: 1.0

---

# Purpose

This document defines the database architecture of FIXAR OS.

The database supports manufacturing traceability, production planning, raw material control, inventory, quality, finance, AI decision support, reporting and long-term scalability.

---

# Database Principles

- Single Source of Truth
- Full Traceability
- Auditability
- Versioning
- Soft Delete
- Multi-Company Ready
- AI-Ready Data Structure

---

# Common Fields

All core tables include:

- ID
- CompanyID
- FactoryID
- Status
- CreatedAt
- CreatedBy
- UpdatedAt
- UpdatedBy
- IsArchived
- Version

---

# DATABASE-001 Companies

## Purpose
Stores legal company entities.

## Fields
- CompanyID
- CompanyCode
- CompanyName
- LegalName
- TaxNumber
- Country
- City
- Address
- CurrencyID
- Status

## Rules
- CompanyCode must be unique.
- Companies cannot be physically deleted.

---

# DATABASE-002 Factories

## Purpose
Stores factory locations.

## Fields
- FactoryID
- CompanyID
- FactoryCode
- FactoryName
- Address
- ManagerEmployeeID
- Status

## Rules
- FactoryCode must be unique within Company.
- Every production record must reference FactoryID.

---

# DATABASE-003 Warehouses

## Purpose
Stores warehouse definitions.

## Fields
- WarehouseID
- CompanyID
- FactoryID
- WarehouseCode
- WarehouseName
- WarehouseType
- Status

## Rules
- WarehouseCode must be unique.
- Inventory cannot exist without WarehouseID.

---

# DATABASE-004 WarehouseLocations

## Purpose
Stores warehouse shelf and location addresses.

## Fields
- LocationID
- WarehouseID
- LocationCode
- Aisle
- Rack
- Shelf
- Bin
- Status

## Rules
- LocationCode must be unique within warehouse.
- Every stored item must reference a location when location tracking is active.

---

# DATABASE-005 Departments

## Purpose
Stores company departments.

## Fields
- DepartmentID
- CompanyID
- FactoryID
- DepartmentCode
- DepartmentName
- ManagerEmployeeID
- Status

## Rules
- Every employee belongs to one department.

---

# DATABASE-006 Employees

## Purpose
Stores employee records.

## Fields
- EmployeeID
- CompanyID
- FactoryID
- DepartmentID
- EmployeeCode
- FirstName
- LastName
- Phone
- Email
- Position
- EmploymentStatus
- Status

## Rules
- EmployeeCode must be unique.
- Every production action must reference an employee where applicable.

---

# DATABASE-007 Users

## Purpose
Stores system user accounts.

## Fields
- UserID
- EmployeeID
- Username
- Email
- PasswordHash
- TwoFactorEnabled
- LastLoginAt
- Status

## Rules
- Username must be unique.
- Inactive users cannot login.

---

# DATABASE-008 Roles

## Purpose
Stores user roles.

## Fields
- RoleID
- RoleCode
- RoleName
- Description
- Status

## Rules
- Permissions are assigned through roles.

---

# DATABASE-009 Permissions

## Purpose
Stores system permissions.

## Fields
- PermissionID
- PermissionCode
- PermissionName
- ModuleName
- Action
- Status

## Rules
- Permissions cannot be deleted after use.

---

# DATABASE-010 UserRoles

## Purpose
Links users to roles.

## Fields
- UserRoleID
- UserID
- RoleID
- AssignedAt
- AssignedBy
- Status

## Rules
- Every user must have at least one role.

---

# DATABASE-011 Customers

## Purpose
Stores customer master data.

## Fields
- CustomerID
- CustomerCode
- CompanyName
- BrandName
- Country
- City
- Address
- TaxNumber
- CurrencyID
- PaymentTerm
- CreditLimit
- RiskScore
- Status

## Rules
- CustomerCode must be unique.
- Customer history cannot be deleted.

---

# DATABASE-012 CustomerContacts

## Purpose
Stores customer contact persons.

## Fields
- ContactID
- CustomerID
- FullName
- Title
- Phone
- Email
- IsPrimary
- Status

## Rules
- One customer may have multiple contacts.

---

# DATABASE-013 CustomerAddresses

## Purpose
Stores customer billing and delivery addresses.

## Fields
- AddressID
- CustomerID
- AddressType
- Country
- City
- AddressLine
- PostalCode
- IsDefault
- Status

## Rules
- Shipments must reference a delivery address.

---

# DATABASE-014 Suppliers

## Purpose
Stores supplier master data.

## Fields
- SupplierID
- SupplierCode
- CompanyName
- Country
- City
- Address
- TaxNumber
- CurrencyID
- PaymentTerm
- QualityScore
- DeliveryScore
- Status

## Rules
- SupplierCode must be unique.
- Supplier performance history is retained.

---

# DATABASE-015 SupplierContacts

## Purpose
Stores supplier contact persons.

## Fields
- ContactID
- SupplierID
- FullName
- Title
- Phone
- Email
- IsPrimary
- Status

---

# DATABASE-016 SupplierMaterials

## Purpose
Links suppliers to materials they provide.

## Fields
- SupplierMaterialID
- SupplierID
- MaterialID
- SupplierMaterialCode
- LeadTimeDays
- MinimumOrderQuantity
- Status

## Rules
- Controlled materials must have approved suppliers.

---

# DATABASE-017 ProductCategories

## Purpose
Stores product category definitions.

## Fields
- ProductCategoryID
- CategoryCode
- CategoryName
- Description
- Status

---

# DATABASE-018 Products

## Purpose
Stores product master data.

## Fields
- ProductID
- ProductCode
- ProductName
- ProductCategoryID
- CustomerID
- Model
- SizeRange
- DefaultRecipeID
- DefaultWeight
- DefaultHardness
- CycleTimeSeconds
- Status

## Rules
- ProductCode must be unique.
- Products must reference approved recipes before production.

---

# DATABASE-019 Recipes

## Purpose
Stores recipe master records.

## Fields
- RecipeID
- RecipeCode
- RecipeName
- ProductType
- Status

## Rules
- Recipes are version controlled.

---

# DATABASE-020 RecipeVersions

## Purpose
Stores recipe versions.

## Fields
- RecipeVersionID
- RecipeID
- VersionNumber
- PolyolType
- IsocyanateType
- CrossKimAmount
- PigmentAmount
- TargetDensity
- TargetHardness
- CuringTimeSeconds
- MixTimeMinutes
- ApprovedBy
- ApprovedAt
- Status

## Rules
- Approved versions cannot be edited.
- Production Lots must reference RecipeVersionID.

---

# DATABASE-021 RecipeMaterials

## Purpose
Stores material composition of recipe versions.

## Fields
- RecipeMaterialID
- RecipeVersionID
- MaterialID
- Quantity
- Unit
- Percentage
- Status

---

# DATABASE-022 RawMaterials

## Purpose
Stores raw material master data.

## Fields
- MaterialID
- MaterialCode
- MaterialName
- MaterialType
- Unit
- ShelfLifeDays
- MinimumStock
- DefaultSupplierID
- Status

## Rules
- MaterialCode must be unique.
- Shelf life must be tracked where applicable.

---

# DATABASE-023 MaterialLots

## Purpose
Stores supplier batch / lot data.

## Fields
- MaterialLotID
- MaterialID
- SupplierID
- SupplierLotNumber
- ProductionDate
- ExpirationDate
- ReceivedDate
- Status

---

# DATABASE-024 Barrels

## Purpose
Stores polyol and isocyanate barrel records.

## Fields
- BarrelID
- BarrelCode
- QRCode
- MaterialID
- MaterialLotID
- SupplierID
- InitialWeight
- RemainingWeight
- ProductionDate
- ExpirationDate
- ReceivedDate
- OpenedAt
- ClosedAt
- StorageLocationID
- Status

## Rules
- Every barrel must have unique QR.
- FIFO must be applied.
- Opened barrel usage must be traceable.

---

# DATABASE-025 FabricLots

## Purpose
Stores fabric roll and lot records.

## Fields
- FabricLotID
- MaterialID
- SupplierID
- Color
- LengthMeters
- ReceivedDate
- LaminationTestStatus
- StorageLocationID
- Status

## Rules
- Fabric cannot be used before lamination test approval.

---

# DATABASE-026 Inventory

## Purpose
Stores current inventory snapshot.

## Fields
- InventoryID
- ItemType
- ItemID
- WarehouseID
- LocationID
- Quantity
- Unit
- LastCalculatedAt
- Status

## Rules
- Inventory is calculated from movements.
- Quantity cannot be negative.

---

# DATABASE-027 InventoryMovements

## Purpose
Stores all stock movements.

## Fields
- MovementID
- ItemType
- ItemID
- WarehouseID
- FromLocationID
- ToLocationID
- MovementType
- Quantity
- Unit
- ReferenceType
- ReferenceID
- MovementDate
- PerformedBy
- Status

## Rules
- Every movement must reference a business document.
- Inventory changes only through movements.

---

# DATABASE-028 PurchaseOrders

## Purpose
Stores supplier purchase orders.

## Fields
- PurchaseOrderID
- PONumber
- SupplierID
- OrderDate
- ExpectedDeliveryDate
- CurrencyID
- ExchangeRate
- PaymentTerm
- Status

## Rules
- PONumber must be unique.
- Purchase Orders require approval.

---

# DATABASE-029 PurchaseOrderItems

## Purpose
Stores purchase order line items.

## Fields
- POItemID
- PurchaseOrderID
- MaterialID
- Quantity
- Unit
- UnitPrice
- TotalPrice
- Status

---

# DATABASE-030 GoodsReceipts

## Purpose
Stores incoming goods receiving records.

## Fields
- GoodsReceiptID
- ReceiptNumber
- PurchaseOrderID
- SupplierID
- ReceivedDate
- ReceivedBy
- QualityStatus
- Status

## Rules
- Materials cannot enter inventory before receiving.

---

# DATABASE-031 Orders

## Purpose
Stores customer sales orders.

## Fields
- OrderID
- OrderNumber
- CustomerID
- OrderDate
- DeliveryDate
- CurrencyID
- ExchangeRate
- PaymentTerm
- Priority
- Status

## Rules
- OrderNumber must be unique.
- Approved orders generate Work Orders.

---

# DATABASE-032 OrderItems

## Purpose
Stores sales order line items.

## Fields
- OrderItemID
- OrderID
- ProductID
- SizeRange
- QuantityPairs
- UnitPrice
- TotalPrice
- DeliveryDate
- Status

---

# DATABASE-033 WorkOrders

## Purpose
Stores production work orders.

## Fields
- WorkOrderID
- WorkOrderNumber
- OrderItemID
- ProductID
- PlannedQuantity
- CompletedQuantity
- RecipeVersionID
- PlannedStartDate
- PlannedEndDate
- Status

## Rules
- WorkOrderNumber must be unique.
- Work Orders must reference approved products and recipes.

---

# DATABASE-034 ProductionLots

## Purpose
Stores production lot records.

## Fields
- ProductionLotID
- LotNumber
- WorkOrderID
- ProductID
- RecipeVersionID
- PolyolBarrelID
- IsocyanateBarrelID
- CrossKimLotID
- FabricLotID
- MachineID
- StationID
- MoldID
- OperatorEmployeeID
- ShiftID
- StartTime
- EndTime
- ProducedQuantity
- ScrapQuantity
- Status

## Rules
- LotNumber must be unique.
- Every lot must be fully traceable.

---

# DATABASE-035 ProductionEvents

## Purpose
Stores production event history.

## Fields
- ProductionEventID
- ProductionLotID
- EventType
- MachineID
- StationID
- EmployeeID
- EventTime
- Description
- Status

## Rules
- Every critical production action creates event record.

---

# DATABASE-036 Machines

## Purpose
Stores production machine records.

## Fields
- MachineID
- MachineCode
- MachineName
- MachineType
- Brand
- Model
- SerialNumber
- FactoryID
- Status

## Rules
- MachineCode must be unique.

---

# DATABASE-037 Stations

## Purpose
Stores machine station records.

## Fields
- StationID
- MachineID
- StationNumber
- CurrentMoldID
- CurrentWorkOrderID
- Status

## Rules
- StationNumber must be unique per machine.

---

# DATABASE-038 Molds

## Purpose
Stores production mold records.

## Fields
- MoldID
- MoldCode
- ProductID
- Size
- Revision
- CavityCount
- CycleTimeSeconds
- TargetWeight
- TotalProductionCount
- LastMaintenanceDate
- Status

## Rules
- MoldCode must be unique.
- Mold usage history must be retained.

---

# DATABASE-039 MoldAssignments

## Purpose
Stores mold-machine-station assignments.

## Fields
- MoldAssignmentID
- MoldID
- MachineID
- StationID
- AssignedAt
- RemovedAt
- AssignedBy
- Status

---

# DATABASE-040 CuttingDies

## Purpose
Stores cutting knife / die records.

## Fields
- CuttingDieID
- CuttingDieCode
- ProductID
- Size
- StorageLocationID
- LastSharpenDate
- UsageCount
- Status

---

# DATABASE-041 CuttingOperations

## Purpose
Stores cutting process records.

## Fields
- CuttingOperationID
- WorkOrderID
- ProductionLotID
- CuttingDieID
- MachineID
- OperatorEmployeeID
- StartTime
- EndTime
- Quantity
- ScrapQuantity
- Status

---

# DATABASE-042 DTFJobs

## Purpose
Stores DTF printing jobs.

## Fields
- DTFJobID
- DTFJobNumber
- WorkOrderID
- ProductionLotID
- ArtworkID
- OperatorEmployeeID
- StartTime
- EndTime
- Quantity
- RejectQuantity
- Status

---

# DATABASE-043 ArtworkLibrary

## Purpose
Stores customer artwork and logo files.

## Fields
- ArtworkID
- CustomerID
- ArtworkCode
- ArtworkName
- Version
- FileID
- ApprovedBy
- ApprovedAt
- Status

---

# DATABASE-044 PackagingOperations

## Purpose
Stores packaging operation records.

## Fields
- PackagingOperationID
- WorkOrderID
- ProductionLotID
- OperatorEmployeeID
- StartTime
- EndTime
- BoxCount
- QuantityPairs
- Status

---

# DATABASE-045 Boxes

## Purpose
Stores finished goods box records.

## Fields
- BoxID
- BoxCode
- QRCodeID
- CustomerID
- OrderID
- WorkOrderID
- ProductionLotID
- WarehouseID
- LocationID
- QuantityPairs
- PackedAt
- PackedBy
- Status

## Rules
- Every box must have unique QR.
- Boxes cannot ship without scan verification.

---

# DATABASE-046 BoxContents

## Purpose
Stores products inside boxes.

## Fields
- BoxContentID
- BoxID
- ProductID
- Size
- QuantityPairs
- ProductionLotID
- Status

---

# DATABASE-047 Shipments

## Purpose
Stores shipment header records.

## Fields
- ShipmentID
- ShipmentNumber
- CustomerID
- DeliveryAddressID
- ShipmentDate
- VehiclePlate
- DriverName
- TransportCompany
- Status

---

# DATABASE-048 ShipmentBoxes

## Purpose
Links boxes to shipments.

## Fields
- ShipmentBoxID
- ShipmentID
- BoxID
- LoadedAt
- LoadedBy
- Status

## Rules
- A box cannot be loaded twice into active shipment.

---

# DATABASE-049 Invoices

## Purpose
Stores sales and purchase invoices.

## Fields
- InvoiceID
- InvoiceNumber
- InvoiceType
- CustomerID
- SupplierID
- OrderID
- PurchaseOrderID
- InvoiceDate
- CurrencyID
- ExchangeRate
- TotalAmount
- Status

---

# DATABASE-050 Payments

## Purpose
Stores payment transactions.

## Fields
- PaymentID
- PaymentNumber
- CustomerID
- SupplierID
- InvoiceID
- PaymentType
- CurrencyID
- ExchangeRate
- Amount
- PaymentDate
- DueDate
- Status

---

# DATABASE-051 Checks

## Purpose
Stores check payment records.

## Fields
- CheckID
- CheckNumber
- CustomerID
- SupplierID
- BankName
- CurrencyID
- ExchangeRate
- Amount
- DueDate
- ReceivedDate
- CollectionDate
- Status

---

# DATABASE-052 ExchangeRates

## Purpose
Stores currency exchange rates.

## Fields
- ExchangeRateID
- CurrencyID
- RateDate
- BuyRate
- SellRate
- Source
- Status

## Rules
- Historical rates cannot be modified.

---

# DATABASE-053 QualityTests

## Purpose
Stores quality test records.

## Fields
- QualityTestID
- TestNumber
- TestType
- RelatedType
- RelatedID
- ProductionLotID
- MaterialLotID
- TestedBy
- TestDate
- TargetValue
- MeasuredValue
- Result
- Notes
- Status

---

# DATABASE-054 QualityDefects

## Purpose
Stores defect records.

## Fields
- DefectID
- DefectCode
- ProductionLotID
- ProductID
- DefectType
- Quantity
- Severity
- RootCause
- ActionTaken
- Status

---

# DATABASE-055 CustomerComplaints

## Purpose
Stores customer complaint records.

## Fields
- ComplaintID
- ComplaintNumber
- CustomerID
- OrderID
- ShipmentID
- ProductID
- ComplaintDate
- Description
- Severity
- Status

---

# DATABASE-056 CAPARecords

## Purpose
Stores corrective and preventive action records.

## Fields
- CAPAID
- CAPANumber
- SourceType
- SourceID
- RootCause
- CorrectiveAction
- PreventiveAction
- ResponsibleEmployeeID
- DueDate
- ClosedAt
- Status

---

# DATABASE-057 MaintenanceRequests

## Purpose
Stores maintenance work orders.

## Fields
- MaintenanceRequestID
- RequestNumber
- MachineID
- MoldID
- RequestType
- Priority
- ReportedBy
- AssignedTo
- StartTime
- EndTime
- RootCause
- Status

---

# DATABASE-058 SpareParts

## Purpose
Stores spare part master data.

## Fields
- SparePartID
- SparePartCode
- SparePartName
- MachineID
- MinimumStock
- CurrentStock
- Unit
- StorageLocationID
- Status

---

# DATABASE-059 Documents

## Purpose
Stores document master records.

## Fields
- DocumentID
- DocumentNumber
- DocumentTitle
- CategoryID
- CurrentVersion
- OwnerEmployeeID
- ApprovalStatus
- Status

---

# DATABASE-060 DocumentVersions

## Purpose
Stores document version history.

## Fields
- DocumentVersionID
- DocumentID
- VersionNumber
- FileID
- UploadedBy
- UploadedAt
- ApprovedBy
- ApprovedAt
- Status

---

# DATABASE-061 Files

## Purpose
Stores file metadata.

## Fields
- FileID
- FileName
- FileType
- FileSize
- StoragePath
- UploadedBy
- UploadedAt
- Status

---

# DATABASE-062 Notifications

## Purpose
Stores system notifications.

## Fields
- NotificationID
- NotificationType
- Priority
- Title
- Message
- RelatedModule
- RelatedID
- CreatedAt
- Status

---

# DATABASE-063 NotificationRecipients

## Purpose
Stores notification recipients.

## Fields
- NotificationRecipientID
- NotificationID
- UserID
- ReadAt
- DeliveryStatus
- Status

---

# DATABASE-064 AuditLogs

## Purpose
Stores user action audit records.

## Fields
- AuditLogID
- UserID
- Action
- ModuleName
- RecordType
- RecordID
- OldValue
- NewValue
- IPAddress
- DeviceInfo
- CreatedAt

## Rules
- Audit logs cannot be modified or deleted.

---

# DATABASE-065 EventLogs

## Purpose
Stores operational event records.

## Fields
- EventLogID
- EventCode
- EventType
- ModuleName
- RelatedType
- RelatedID
- UserID
- MachineID
- StationID
- Description
- CreatedAt

## Rules
- Event logs are permanent.

---

# DATABASE-066 QRCodes

## Purpose
Stores QR code identities.

## Fields
- QRCodeID
- QRCodeValue
- EntityType
- EntityID
- GeneratedAt
- GeneratedBy
- Status

## Rules
- QRCodeValue must be globally unique.
- QR codes carry identity only, not business data.

---

# DATABASE-067 QRScans

## Purpose
Stores QR scan history.

## Fields
- QRScanID
- QRCodeID
- ScannedBy
- ScannedAt
- ScanLocation
- DeviceID
- Result
- Status

---

# DATABASE-068 KPIs

## Purpose
Stores KPI definitions.

## Fields
- KPIID
- KPICode
- KPIName
- DepartmentID
- Formula
- TargetValue
- Unit
- Status

---

# DATABASE-069 KPIResults

## Purpose
Stores calculated KPI values.

## Fields
- KPIResultID
- KPIID
- PeriodStart
- PeriodEnd
- ActualValue
- TargetValue
- CalculatedAt
- Status

---

# DATABASE-070 Dashboards

## Purpose
Stores dashboard definitions.

## Fields
- DashboardID
- DashboardCode
- DashboardName
- OwnerUserID
- Visibility
- Status

---

# DATABASE-071 DashboardWidgets

## Purpose
Stores dashboard widgets.

## Fields
- WidgetID
- DashboardID
- WidgetType
- DataSource
- PositionX
- PositionY
- Width
- Height
- Configuration
- Status

---

# DATABASE-072 AIRecommendations

## Purpose
Stores AI recommendations.

## Fields
- RecommendationID
- RecommendationType
- Title
- Description
- Priority
- ConfidenceScore
- RelatedModule
- RelatedID
- SuggestedAction
- Status
- GeneratedAt
- ReviewedBy
- ReviewedAt

## Rules
- AI recommendations never execute automatically.

---

# DATABASE-073 AIModels

## Purpose
Stores AI model registry.

## Fields
- AIModelID
- ModelCode
- ModelName
- ModelType
- Version
- Status

---

# DATABASE-074 ForecastResults

## Purpose
Stores AI forecast outputs.

## Fields
- ForecastID
- ForecastType
- RelatedModule
- RelatedID
- ForecastDate
- ForecastValue
- ConfidenceScore
- ModelVersion
- Status

---

# DATABASE-075 APIClients

## Purpose
Stores API client applications.

## Fields
- APIClientID
- ClientName
- ClientKey
- SecretHash
- Status

---

# DATABASE-076 APIRequests

## Purpose
Stores API request logs.

## Fields
- APIRequestID
- APIClientID
- Endpoint
- Method
- RequestTime
- ResponseTime
- StatusCode
- Status

---

# DATABASE-077 Integrations

## Purpose
Stores external system integrations.

## Fields
- IntegrationID
- IntegrationCode
- IntegrationName
- IntegrationType
- EndpointURL
- AuthType
- Status

---

# DATABASE-078 SyncHistory

## Purpose
Stores integration synchronization history.

## Fields
- SyncHistoryID
- IntegrationID
- SyncType
- StartedAt
- FinishedAt
- RecordsProcessed
- Result
- Status

---

# DATABASE-079 Backups

## Purpose
Stores backup records.

## Fields
- BackupID
- BackupType
- BackupPath
- BackupSize
- StartedAt
- FinishedAt
- VerificationStatus
- Status

---

# DATABASE-080 SystemSettings

## Purpose
Stores system-wide settings.

## Fields
- SettingID
- SettingKey
- SettingValue
- SettingType
- IsEncrypted
- Status

## Rules
- Critical settings require administrator approval.

---

# Core Traceability Chain

Customer

→ Order

→ OrderItem

→ WorkOrder

→ ProductionLot

→ ProductionEvents

→ QualityTests

→ CuttingOperations

→ DTFJobs

→ PackagingOperations

→ Boxes

→ ShipmentBoxes

→ Shipments

→ Invoice

→ Payment

---

# Raw Material Traceability Chain

Supplier

→ PurchaseOrder

→ GoodsReceipt

→ MaterialLot

→ Barrel / FabricLot

→ ProductionLot

→ Box

→ Shipment

→ Customer

---

# Finance Traceability Chain

Order

→ Invoice

→ Payment

→ Check

→ ExchangeRate

→ ProfitAnalysis

---

# Audit Rule

No critical business transaction is valid unless it can be traced through:

- EventLogs
- AuditLogs
- UserID
- Timestamp
- Related Record ID

---

# Database Architecture Status

Status: Approved Draft

Version: 1.0
