export type MaintenanceRow = {
  [key: string]: unknown;
  id: string; maintenanceAssetId: string; requestNumber: string; maintenanceWorkOrderNumber: string;
  planCode: string; assetCode: string; assetName: string; asset: string; title: string; name: string;
  priority: string; defaultPriority: string; status: string; workType: string; requestType: string;
  description: string; productionImpact: string; plannedStart: string; plannedEnd: string; reportedAt: string;
  nextDueDate: string; startDate: string; downtimeStartedAt: string; frequencyType: string;
  frequencyValue: number | string; estimatedDurationMinutes: number | string; advanceCreateDays: number | string;
  machineStopped: boolean; requiresProductionStop: boolean; autoCreateWorkOrder: boolean; isActive: boolean;
  isRequired: boolean; isCompleted: boolean; isReversed: boolean; outOfTolerance: boolean;
  assetType: string; criticality: string; maintenanceStrategy: string; targetId: string; manufacturer: string;
  model: string; serialNumber: string; location: string; notes: string; workOrderId: string;
  templateItemId: string; itemText: string; itemType: string; sequence: number; numericValue: number | string;
  textValue: string; passFail: boolean; unit: string; stockItem: string; quantity: number;
  openRequests: number; workOrders: number; downtimeMinutes: number; totalCost: number;
};
