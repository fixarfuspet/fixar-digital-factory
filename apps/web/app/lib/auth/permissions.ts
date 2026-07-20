export const rolePermissions: Record<string, readonly string[]> = {
  CEO: ["*"],
  "Factory Manager": ["production", "work-orders", "reservations", "consumption", "quality", "cutting", "boxes", "warehouse", "shipments", "stocks", "lots", "containers", "materials", "products", "recipes", "maintenance", "maintenance-request", "maintenance-plan", "costs"],
  "Production Manager": ["production", "work-orders", "reservations", "consumption", "quality", "cutting", "boxes", "warehouse", "shipments", "stocks", "lots", "containers", "materials", "products", "recipes", "maintenance", "maintenance-request", "maintenance-plan", "costs", "profitability"],
  "Production Supervisor": ["production", "work-orders", "quality", "cutting", "boxes", "warehouse", "maintenance-request"],
  "Production Operator": ["production", "consumption", "maintenance-request"],
  InjectionOperator: ["production", "consumption", "maintenance-request"],
  CuttingOperator: ["cutting", "boxes", "traceability"],
  "Warehouse Manager": ["stocks", "lots", "containers", "warehouse", "shipments", "boxes", "traceability", "maintenance"],
  "Warehouse Operator": ["stocks", "lots", "containers", "warehouse", "shipments", "boxes", "traceability", "maintenance"],
  QualityOperator: ["quality", "lots"],
  "Purchasing Manager": ["suppliers", "purchases", "materials", "lots", "supplier-finance"],
  Purchasing: ["suppliers", "purchases", "materials", "lots", "supplier-finance"],
  "Maintenance Manager": ["maintenance", "maintenance-request", "maintenance-plan", "stocks", "costs"],
  "Maintenance Technician": ["maintenance", "maintenance-request", "stocks"],
  "Quality Manager": ["quality", "lots", "maintenance-request"],
  "Quality Inspector": ["quality", "lots", "maintenance-request"],
  "Sales Manager": ["customers", "orders", "customer-finance", "cheques"],
  "Finance Manager": ["orders", "purchases", "costs", "profitability", "customer-finance", "financial-accounts", "cheques", "cash-flow", "supplier-finance", "maintenance"],
  Finance: ["orders", "purchases", "costs", "profitability", "customer-finance", "financial-accounts", "cheques", "cash-flow", "supplier-finance", "maintenance"],
  Viewer: ["read"],
};

export function can(roles: readonly string[], permission: string): boolean {
  return roles.some((role) => rolePermissions[role]?.some((value) => value === "*" || value === permission));
}
