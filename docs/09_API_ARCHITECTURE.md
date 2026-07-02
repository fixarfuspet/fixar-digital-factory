# FIXAR OS

# API Architecture

Version: 1.0

---

# Purpose

This document defines the API architecture of FIXAR OS.

APIs connect frontend screens, mobile apps, AI services, hardware integrations, database operations and external systems.

---

# API Principles

- Secure by default
- REST-first architecture
- JSON request / response
- Role-based access control
- Audit logging for critical actions
- Versioned endpoints
- Standard error responses
- AI-ready data access
- Real-time updates where needed

---

# Base API URL

/api/v1

---

# Authentication

## Login

POST /auth/login

## Logout

POST /auth/logout

## Refresh Token

POST /auth/refresh-token

## MFA Verify

POST /auth/mfa/verify

## Forgot Password

POST /auth/forgot-password

## Reset Password

POST /auth/reset-password

---

# Standard Response Format

## Success

{
  "success": true,
  "data": {},
  "message": "Operation completed successfully"
}

## Error

{
  "success": false,
  "errorCode": "ERROR_CODE",
  "message": "Error message"
}

---

# Common HTTP Methods

GET = Read data

POST = Create data

PUT = Update full record

PATCH = Update partial record

DELETE = Archive record

---

# API-001 Company APIs

GET /companies

GET /companies/{companyId}

POST /companies

PUT /companies/{companyId}

DELETE /companies/{companyId}

---

# API-002 Factory APIs

GET /factories

GET /factories/{factoryId}

POST /factories

PUT /factories/{factoryId}

DELETE /factories/{factoryId}

---

# API-003 User APIs

GET /users

GET /users/{userId}

POST /users

PUT /users/{userId}

PATCH /users/{userId}/status

DELETE /users/{userId}

---

# API-004 Role APIs

GET /roles

GET /roles/{roleId}

POST /roles

PUT /roles/{roleId}

DELETE /roles/{roleId}

---

# API-005 Permission APIs

GET /permissions

GET /permissions/matrix

POST /permissions/assign

POST /permissions/remove

---

# API-006 Customer APIs

GET /customers

GET /customers/{customerId}

POST /customers

PUT /customers/{customerId}

DELETE /customers/{customerId}

GET /customers/{customerId}/orders

GET /customers/{customerId}/shipments

GET /customers/{customerId}/payments

GET /customers/{customerId}/complaints

---

# API-007 Customer Contact APIs

GET /customers/{customerId}/contacts

POST /customers/{customerId}/contacts

PUT /customer-contacts/{contactId}

DELETE /customer-contacts/{contactId}

---

# API-008 Supplier APIs

GET /suppliers

GET /suppliers/{supplierId}

POST /suppliers

PUT /suppliers/{supplierId}

DELETE /suppliers/{supplierId}

GET /suppliers/{supplierId}/purchase-orders

GET /suppliers/{supplierId}/performance

---

# API-009 Product APIs

GET /products

GET /products/{productId}

POST /products

PUT /products/{productId}

DELETE /products/{productId}

GET /products/{productId}/recipes

GET /products/{productId}/production-history

---

# API-010 Raw Material APIs

GET /raw-materials

GET /raw-materials/{materialId}

POST /raw-materials

PUT /raw-materials/{materialId}

DELETE /raw-materials/{materialId}

GET /raw-materials/low-stock

GET /raw-materials/expiring

---

# API-011 Barrel APIs

GET /barrels

GET /barrels/{barrelId}

POST /barrels

PUT /barrels/{barrelId}

PATCH /barrels/{barrelId}/open

PATCH /barrels/{barrelId}/close

GET /barrels/{barrelId}/traceability

---

# API-012 Fabric Lot APIs

GET /fabric-lots

GET /fabric-lots/{fabricLotId}

POST /fabric-lots

PUT /fabric-lots/{fabricLotId}

PATCH /fabric-lots/{fabricLotId}/approve

PATCH /fabric-lots/{fabricLotId}/reject

---

# API-013 Inventory APIs

GET /inventory

GET /inventory/current

GET /inventory/{inventoryId}

GET /inventory/movements

POST /inventory/movements

POST /inventory/adjustment

GET /inventory/low-stock

GET /inventory/valuation

---

# API-014 Warehouse APIs

GET /warehouses

GET /warehouses/{warehouseId}

POST /warehouses

PUT /warehouses/{warehouseId}

GET /warehouses/{warehouseId}/locations

POST /warehouses/{warehouseId}/locations

POST /warehouse/transfer

POST /warehouse/pick

---

# API-015 QR APIs

GET /qr/{qrCode}

POST /qr/generate

POST /qr/scan

GET /qr/{qrCode}/history

GET /qr/{qrCode}/traceability

---

# API-016 Order APIs

GET /orders

GET /orders/{orderId}

POST /orders

PUT /orders/{orderId}

PATCH /orders/{orderId}/approve

PATCH /orders/{orderId}/cancel

GET /orders/{orderId}/timeline

GET /orders/{orderId}/profitability

---

# API-017 Order Item APIs

GET /orders/{orderId}/items

POST /orders/{orderId}/items

PUT /order-items/{orderItemId}

DELETE /order-items/{orderItemId}

---

# API-018 Work Order APIs

GET /work-orders

GET /work-orders/{workOrderId}

POST /work-orders

PUT /work-orders/{workOrderId}

PATCH /work-orders/{workOrderId}/release

PATCH /work-orders/{workOrderId}/complete

GET /work-orders/{workOrderId}/materials

GET /work-orders/{workOrderId}/timeline

---

# API-019 Production Planning APIs

GET /production/plans

POST /production/plans

PUT /production/plans/{planId}

PATCH /production/plans/{planId}/release

GET /production/capacity

GET /production/schedule

---

# API-020 Production APIs

GET /production/lots

GET /production/lots/{lotId}

POST /production/lots

PATCH /production/lots/{lotId}/start

PATCH /production/lots/{lotId}/pause

PATCH /production/lots/{lotId}/resume

PATCH /production/lots/{lotId}/complete

GET /production/lots/{lotId}/traceability

GET /production/lots/{lotId}/events

---

# API-021 Production Event APIs

GET /production/events

POST /production/events

GET /production/events/{eventId}

---

# API-022 Machine APIs

GET /machines

GET /machines/{machineId}

POST /machines

PUT /machines/{machineId}

PATCH /machines/{machineId}/status

GET /machines/{machineId}/events

GET /machines/{machineId}/oee

GET /machines/{machineId}/maintenance-history

---

# API-023 Station APIs

GET /stations

GET /stations/{stationId}

POST /stations

PUT /stations/{stationId}

PATCH /stations/{stationId}/assign-mold

PATCH /stations/{stationId}/activate

PATCH /stations/{stationId}/deactivate

---

# API-024 Mold APIs

GET /molds

GET /molds/{moldId}

POST /molds

PUT /molds/{moldId}

PATCH /molds/{moldId}/maintenance

GET /molds/{moldId}/production-history

GET /molds/{moldId}/traceability

---

# API-025 Recipe APIs

GET /recipes

GET /recipes/{recipeId}

POST /recipes

PUT /recipes/{recipeId}

GET /recipes/{recipeId}/versions

POST /recipes/{recipeId}/versions

PATCH /recipe-versions/{versionId}/approve

PATCH /recipe-versions/{versionId}/archive

---

# API-026 Cutting APIs

GET /cutting/operations

GET /cutting/operations/{operationId}

POST /cutting/operations

PATCH /cutting/operations/{operationId}/start

PATCH /cutting/operations/{operationId}/complete

POST /cutting/defects

POST /cutting/scrap

---

# API-027 DTF APIs

GET /dtf/jobs

GET /dtf/jobs/{dtfJobId}

POST /dtf/jobs

PATCH /dtf/jobs/{dtfJobId}/start

PATCH /dtf/jobs/{dtfJobId}/complete

POST /dtf/reprint

GET /dtf/artworks

POST /dtf/artworks

---

# API-028 Packaging APIs

GET /packaging/operations

GET /packaging/operations/{packagingId}

POST /packaging/operations

PATCH /packaging/operations/{packagingId}/start

PATCH /packaging/operations/{packagingId}/complete

POST /packaging/boxes

GET /packaging/boxes/{boxId}

---

# API-029 Box APIs

GET /boxes

GET /boxes/{boxId}

GET /boxes/{boxId}/contents

GET /boxes/{boxId}/traceability

PATCH /boxes/{boxId}/status

---

# API-030 Shipment APIs

GET /shipments

GET /shipments/{shipmentId}

POST /shipments

PUT /shipments/{shipmentId}

PATCH /shipments/{shipmentId}/prepare

PATCH /shipments/{shipmentId}/load

PATCH /shipments/{shipmentId}/dispatch

PATCH /shipments/{shipmentId}/deliver

GET /shipments/{shipmentId}/documents

---

# API-031 Quality APIs

GET /quality/tests

GET /quality/tests/{testId}

POST /quality/tests

PATCH /quality/tests/{testId}/approve

PATCH /quality/tests/{testId}/reject

GET /quality/defects

POST /quality/defects

GET /quality/reports

---

# API-032 Customer Complaint APIs

GET /complaints

GET /complaints/{complaintId}

POST /complaints

PUT /complaints/{complaintId}

PATCH /complaints/{complaintId}/close

GET /complaints/{complaintId}/timeline

---

# API-033 CAPA APIs

GET /capa

GET /capa/{capaId}

POST /capa

PUT /capa/{capaId}

PATCH /capa/{capaId}/close

---

# API-034 Maintenance APIs

GET /maintenance/requests

GET /maintenance/requests/{requestId}

POST /maintenance/requests

PATCH /maintenance/requests/{requestId}/assign

PATCH /maintenance/requests/{requestId}/start

PATCH /maintenance/requests/{requestId}/complete

GET /maintenance/calendar

---

# API-035 Spare Part APIs

GET /spare-parts

GET /spare-parts/{sparePartId}

POST /spare-parts

PUT /spare-parts/{sparePartId}

POST /spare-parts/consume

---

# API-036 Finance APIs

GET /finance/dashboard

GET /finance/cash-flow

GET /finance/profitability

GET /finance/customer-balances

GET /finance/supplier-balances

---

# API-037 Invoice APIs

GET /invoices

GET /invoices/{invoiceId}

POST /invoices

PATCH /invoices/{invoiceId}/approve

PATCH /invoices/{invoiceId}/cancel

---

# API-038 Payment APIs

GET /payments

GET /payments/{paymentId}

POST /payments

PUT /payments/{paymentId}

GET /payments/overdue

---

# API-039 Check APIs

GET /checks

GET /checks/{checkId}

POST /checks

PATCH /checks/{checkId}/collect

PATCH /checks/{checkId}/return

GET /checks/due-soon

---

# API-040 Exchange Rate APIs

GET /exchange-rates

GET /exchange-rates/current

POST /exchange-rates/sync

GET /exchange-rates/history

---

# API-041 Cost APIs

GET /cost/product/{productId}

GET /cost/order/{orderId}

GET /cost/production-lot/{lotId}

POST /cost/calculate

GET /cost/profitability

---

# API-042 HR APIs

GET /employees

GET /employees/{employeeId}

POST /employees

PUT /employees/{employeeId}

GET /employees/{employeeId}/attendance

GET /employees/{employeeId}/training

---

# API-043 Shift APIs

GET /shifts

POST /shifts

PUT /shifts/{shiftId}

GET /shifts/calendar

---

# API-044 Attendance APIs

GET /attendance

POST /attendance/check-in

POST /attendance/check-out

GET /attendance/report

---

# API-045 Document APIs

GET /documents

GET /documents/{documentId}

POST /documents

POST /documents/{documentId}/versions

PATCH /documents/{documentId}/approve

GET /documents/{documentId}/download

---

# API-046 File APIs

POST /files/upload

GET /files/{fileId}

DELETE /files/{fileId}

---

# API-047 Notification APIs

GET /notifications

GET /notifications/unread

PATCH /notifications/{notificationId}/read

POST /notifications/send

---

# API-048 Audit APIs

GET /audit-logs

GET /audit-logs/{auditLogId}

GET /audit-logs/by-user/{userId}

GET /audit-logs/by-record/{recordType}/{recordId}

---

# API-049 Event Log APIs

GET /event-logs

GET /event-logs/{eventLogId}

POST /event-logs

---

# API-050 KPI APIs

GET /kpis

GET /kpis/{kpiId}

POST /kpis

PUT /kpis/{kpiId}

GET /kpis/results

POST /kpis/calculate

---

# API-051 Dashboard APIs

GET /dashboards

GET /dashboards/{dashboardId}

POST /dashboards

PUT /dashboards/{dashboardId}

GET /dashboards/ceo

GET /dashboards/production

GET /dashboards/warehouse

GET /dashboards/finance

---

# API-052 Report APIs

GET /reports

POST /reports/generate

GET /reports/{reportId}

GET /reports/{reportId}/download

POST /reports/schedule

---

# API-053 AI Recommendation APIs

GET /ai/recommendations

GET /ai/recommendations/{recommendationId}

PATCH /ai/recommendations/{recommendationId}/accept

PATCH /ai/recommendations/{recommendationId}/reject

---

# API-054 AI Forecast APIs

GET /ai/forecasts

POST /ai/forecasts/generate

GET /ai/forecasts/{forecastId}

---

# API-055 AI Chat APIs

POST /ai/chat

GET /ai/chat/history

POST /ai/chat/feedback

---

# API-056 Digital Twin APIs

GET /digital-twin/factory-state

GET /digital-twin/machines

GET /digital-twin/simulations

POST /digital-twin/simulations

GET /digital-twin/simulations/{simulationId}

---

# API-057 Integration APIs

GET /integrations

GET /integrations/{integrationId}

POST /integrations

PUT /integrations/{integrationId}

POST /integrations/{integrationId}/test

POST /integrations/{integrationId}/sync

---

# API-058 Backup APIs

GET /backups

POST /backups/create

POST /backups/{backupId}/verify

POST /backups/{backupId}/restore

---

# API-059 System Health APIs

GET /system/health

GET /system/services

GET /system/database

GET /system/api-status

GET /system/logs

---

# API-060 Settings APIs

GET /settings

PUT /settings

GET /settings/company

PUT /settings/company

GET /settings/security

PUT /settings/security

---

# Real-Time APIs

FIXAR OS uses real-time communication for:

- Live Production
- Machine Status
- Station Status
- Notifications
- AI Alerts
- Dashboard Updates
- Warehouse QR Scans

Recommended technology:

- WebSocket
- Server-Sent Events
- Background Jobs

---

# WebSocket Channels

production.live

machine.status

station.status

warehouse.scans

notifications

ai.alerts

dashboard.ceo

dashboard.production

---

# API Security Rules

## API-BR-001

Every API request must be authenticated unless explicitly public.

## API-BR-002

Every critical API request must be logged.

## API-BR-003

Financial APIs require finance permission.

## API-BR-004

Production control APIs require production authorization.

## API-BR-005

AI APIs never execute operational actions automatically.

## API-BR-006

Delete requests archive records instead of physical deletion.

## API-BR-007

All API responses must follow standard response format.

---

# API Status

Status: Approved Draft

Version: 1.0
