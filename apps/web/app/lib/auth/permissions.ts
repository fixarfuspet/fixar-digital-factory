export const rolePermissions: Record<string, readonly string[]> = {
  CEO: ["*"],
  "Production Manager": ["production", "work-orders", "reservations", "consumption", "quality", "cutting", "boxes", "warehouse", "shipments", "costs"],
  InjectionOperator: ["production", "consumption"],
  CuttingOperator: ["cutting", "boxes", "traceability"],
  "Warehouse Operator": ["stocks", "lots", "containers", "warehouse", "shipments"],
  QualityOperator: ["quality", "lots"],
  Purchasing: ["suppliers", "purchases", "materials", "lots"],
  Finance: ["orders", "purchases", "costs", "customer-finance", "financial-accounts", "cheques", "cash-flow", "supplier-finance"],
  Viewer: ["read"],
};

export function can(roles: readonly string[], permission: string): boolean {
  return roles.some((role) => rolePermissions[role]?.some((value) => value === "*" || value === permission));
}
