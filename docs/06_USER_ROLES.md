# FIXAR OS

# User Roles & Permission Architecture

Version: 1.0

---

# Purpose

This document defines all user roles, responsibilities and permission structures within FIXAR OS.

The objective is to ensure that every user only has access to the functions required for their responsibilities while maintaining security, accountability and operational efficiency.

---

# Access Control Model

FIXAR OS uses Role-Based Access Control (RBAC).

Every permission is assigned through roles rather than directly to users.

Permission Types

- View
- Create
- Edit
- Delete
- Approve
- Reject
- Export
- Print
- Execute
- Configure
- Manage

---

# Organization Hierarchy

Board

↓

CEO

↓

Factory Manager

↓

Department Managers

↓

Supervisors

↓

Operators

---

# USER-001 CEO

Full system access.

Responsibilities

- Executive Dashboard
- Company KPIs
- Financial Reports
- Production Performance
- AI Executive Reports
- User Approval
- Strategic Decisions

Permissions

- Full Read
- Full Create
- Full Update
- Full Approval
- Financial Approval
- User Management
- AI Access
- Reports
- System Settings

---

# USER-002 Factory Manager

Responsibilities

- Production
- Maintenance
- Warehouse
- Planning
- Quality Coordination

Permissions

- Production Management
- Machine Management
- Production Planning
- Inventory View
- Maintenance Approval
- Quality Reports

Cannot

- Modify company settings
- Delete financial records

---

# USER-003 Production Manager

Responsibilities

- Production Orders
- Recipes
- Machines
- Daily Planning
- Operators

Permissions

- Production
- Recipes
- Work Orders
- Production Reports
- Machine Status

Cannot

- Finance
- HR Administration

---

# USER-004 Warehouse Manager

Responsibilities

- Inventory
- Receiving
- Shipping
- Transfers
- Barcode Operations

Permissions

- Inventory Management
- Warehouse Transfers
- Shipment Preparation
- QR Operations
- Label Printing

---

# USER-005 Purchasing Manager

Responsibilities

- Suppliers
- Purchase Orders
- Raw Materials
- Supplier Performance

Permissions

- Purchase Orders
- Supplier Management
- Material Approval

---

# USER-006 Finance Manager

Responsibilities

- Accounting
- Cash Flow
- Payments
- Invoices
- Profitability

Permissions

- Financial Reports
- Payments
- Receivables
- Payables
- Currency Rates

---

# USER-007 Quality Manager

Responsibilities

- Inspections
- CAPA
- Complaints
- Testing

Permissions

- Quality Records
- Defect Approval
- Quality Reports
- CAPA

---

# USER-008 Maintenance Manager

Responsibilities

- Machines
- Preventive Maintenance
- Spare Parts
- Repairs

Permissions

- Maintenance Planning
- Work Orders
- Spare Parts
- Machine Status

---

# USER-009 HR Manager

Responsibilities

- Employees
- Attendance
- Leave
- Training

Permissions

- Employee Records
- Attendance
- Shift Planning
- Training Records

---

# USER-010 Sales Manager

Responsibilities

- Customers
- Quotations
- Orders
- CRM

Permissions

- Customer Management
- Quotations
- Orders
- Sales Reports

---

# USER-011 Production Supervisor

Responsibilities

- Daily Production
- Shift Monitoring
- Operator Control

Permissions

- Production View
- Shift Reports
- Downtime Entry
- Production Approval

---

# USER-012 Warehouse Operator

Responsibilities

- Receiving
- Picking
- Packing
- QR Scanning

Permissions

- Inventory View
- QR Scan
- Shipment Operations

Cannot

- Modify inventory manually

---

# USER-013 Production Operator

Responsibilities

- Production Execution
- QR Scanning
- Machine Operation

Permissions

- Assigned Work Orders
- Production Entry
- Scrap Entry

Cannot

- Edit Recipes
- Approve Production

---

# USER-014 Quality Inspector

Responsibilities

- Product Inspection
- Defect Recording

Permissions

- Inspection
- Test Results
- NCR Creation

---

# USER-015 Maintenance Technician

Responsibilities

- Repair Machines
- Preventive Maintenance

Permissions

- Maintenance Tasks
- Machine Logs

---

# USER-016 Guest

Permissions

- Read-only access to assigned dashboards

---

# Permission Matrix

Modules

- Dashboard
- CRM
- Orders
- Production
- Warehouse
- Inventory
- Purchasing
- Finance
- HR
- Quality
- Maintenance
- Reports
- AI Center
- Settings

Each module supports

- View
- Create
- Edit
- Approve
- Delete
- Export

---

# Approval Levels

Level 1

Supervisor

Level 2

Department Manager

Level 3

Factory Manager

Level 4

CEO

---

# Separation of Duties

The same user cannot

- Create and approve the same payment
- Create and approve the same purchase order
- Create and approve the same recipe
- Create and approve the same user account

---

# Audit Requirements

Every permission change stores

- User
- Role
- Previous Permission
- New Permission
- Date
- Time
- Administrator

---

# Business Rules

## ROLE-BR-001

Every user must have at least one role.

## ROLE-BR-002

Every permission change must be audited.

## ROLE-BR-003

No user receives unrestricted administrator access by default.

## ROLE-BR-004

Financial approvals require Finance Manager or CEO authorization.

## ROLE-BR-005

Recipe approvals require Production Manager approval.

## ROLE-BR-006

Production Operators cannot modify recipes.

## ROLE-BR-007

Warehouse Operators cannot manually adjust inventory quantities.

## ROLE-BR-008

AI recommendations require human approval.

---

# Status

Approved Draft

Version: 1.0
