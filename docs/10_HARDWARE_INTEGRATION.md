# FIXAR OS

# Hardware Integration

Version: 1.0

---

# Purpose

This document defines all hardware devices integrated with FIXAR OS.

The goal is to transform the production facility into a fully connected Smart Factory where every machine, sensor, operator and material movement is digitally monitored in real time.

Hardware integration supports:

- Production
- Inventory
- Maintenance
- Quality
- Energy Monitoring
- Traceability
- AI Analysis
- Digital Twin
- Executive Dashboards

---

# Smart Factory Vision

Every physical event inside the factory must generate a digital event.

Examples

Operator starts production

↓

Machine sends signal

↓

Production counter updates

↓

Inventory decreases

↓

Finished goods increase

↓

Dashboard updates

↓

AI analyzes efficiency

↓

Audit log stored

---

# Hardware Categories

FIXAR OS supports:

- Production Machines
- PLC Controllers
- Industrial PCs
- Touch Tablets
- Barcode Readers
- QR Readers
- RFID Readers
- RFID Gates
- Thermal Label Printers
- Barcode Printers
- Network Printers
- Digital Scales
- Smart Cameras
- Vision Systems
- Temperature Sensors
- Humidity Sensors
- Pressure Sensors
- Flow Sensors
- Vibration Sensors
- Power Meters
- Compressors
- Air Quality Sensors
- Smart Lighting
- IoT Gateways
- OPC-UA Servers
- MQTT Brokers
- Modbus Devices
- TCP/IP Devices

---

# Production Machine Integration

Every production machine shall have:

Machine ID

Machine Type

PLC Address

Current Status

Current Recipe

Current Mold

Current Product

Operator

Current Shift

Cycle Counter

Current Temperature

Current Pressure

Alarm Status

Maintenance Status

Power Consumption

---

# Production Machine States

Idle

Setup

Heating

Running

Paused

Alarm

Maintenance

Cleaning

Calibration

Shutdown

Offline

---

# Machine Communication

Supported protocols

OPC-UA

Modbus TCP

Modbus RTU

Ethernet/IP

Profinet

MQTT

REST API

TCP Socket

Serial RS232

Serial RS485

---

# PLC Integration

Each PLC must provide

Machine Status

Start Signal

Stop Signal

Cycle Complete

Alarm

Emergency Stop

Temperature

Pressure

Digital Inputs

Digital Outputs

Analog Inputs

Analog Outputs

Production Counter

Current Recipe

---

# Production Cycle

PLC

↓

Cycle Finished

↓

FIXAR API

↓

Production Event

↓

Database

↓

Dashboard

↓

AI Analysis

↓

Digital Twin

---

# Machine Data Collection

Collected every cycle

Cycle Time

Machine Status

Temperature

Pressure

Operator

Recipe Version

Raw Material Batch

Current Mold

Product Code

Shift

Timestamp

---

# Mold Monitoring

Each mold stores

Mold ID

Current Machine

Current Product

Current Recipe

Production Counter

Lifetime Counter

Maintenance Counter

Cleaning Counter

Current Temperature

Cycle Time

Status

---

# Mold Events

Installed

Removed

Cleaned

Maintained

Calibrated

Approved

Rejected

Retired

---

# Temperature Sensors

Installed on

Production Machine

Mold

Heating System

Warehouse

Raw Material Storage

Compressor Room

Server Room

Electrical Cabinet

---

# Temperature Data

Current Temperature

Minimum

Maximum

Average

Alarm Threshold

Timestamp

Sensor ID

---

# Pressure Sensors

Installed on

Injection Unit

Hydraulic System

Compressed Air

Pipes

Mixing Unit

Storage Tank

---

# Pressure Data

Current Pressure

Minimum

Maximum

Average

Pressure Alarm

Timestamp

---

# Flow Sensors

Installed on

Polyol Line

Isocyanate Line

Cleaning System

Cooling System

Water System

---

# Flow Data

Current Flow

Daily Flow

Hourly Flow

Total Consumption

Flow Alarm

---

# Vibration Sensors

Installed on

Production Machines

Compressors

Motors

Pumps

Fans

---

# AI Maintenance Prediction

Collected values

Temperature

Pressure

Current

Voltage

Power

Vibration

Noise

Cycle Count

Maintenance History

AI predicts

Bearing Failure

Motor Failure

Pump Failure

Hydraulic Failure

Compressor Failure

Expected Remaining Life

---

# Compressor Monitoring

Monitor

Pressure

Temperature

Oil Level

Running Hours

Power

Energy Consumption

Maintenance Interval

Alarm Status

---

# Electrical Energy Monitoring

Every major machine reports

Voltage

Current

Power

Reactive Power

Frequency

Energy Consumption

Power Factor

Daily Consumption

Monthly Consumption

---

# Energy Dashboard

Machine Ranking

Highest Consumers

Production Energy

Idle Energy

Energy per Pair

Energy Cost

Forecast

---

# Industrial Tablets

Supported Locations

Production

Warehouse

Packaging

Maintenance

Quality

Shipping

Receiving

Each tablet supports

QR Scan

Production Entry

Inspection

Approvals

Notifications

AI Assistant

---

# Industrial PCs

Installed on

Production Line

Warehouse

Quality Lab

Maintenance

Packaging

Executive Office

---

# QR Code System

QR codes identify

Products

Boxes

Machines

Molds

Operators

Raw Materials

Fabric Rolls

Production Lots

Shipments

Warehouses

Locations

---

# QR Scan Flow

Scan

↓

Validation

↓

Database

↓

Inventory Update

↓

Traceability

↓

Audit Log

↓

Dashboard Refresh

---

# RFID System

Supported Objects

Raw Materials

Finished Goods

Boxes

Pallets

Reusable Containers

Equipment

---

# RFID Events

Enter Warehouse

Leave Warehouse

Move Location

Shipment

Receiving

Production Consumption

---

# Thermal Label Printers

Supported Labels

Box Label

Shipment Label

Product Label

QR Label

RFID Label

Warehouse Label

Pallet Label

---

# Smart Scale Integration

Supported Functions

Receiving Weight

Production Weight

Packaging Weight

Shipping Weight

Inventory Verification

Calibration

---

# Camera Integration

Supported Cameras

IP Camera

Industrial Camera

AI Camera

Vision Camera

USB Camera

---

# Vision System

Supports

Product Inspection

Color Verification

Label Verification

Defect Detection

Dimension Check

Presence Detection

AI Classification

---

# AI Vision

Detect

Surface Defects

Fabric Problems

Wrong Labels

Missing Parts

Wrong Packaging

Damaged Boxes

---

# Alarm System

Alarm Levels

Information

Warning

Critical

Emergency

Alarm Sources

PLC

Temperature

Pressure

Power

Machine

Quality

Inventory

Security

AI

---

# Alarm Actions

Popup

SMS

Email

Mobile Notification

Telegram

Microsoft Teams

Dashboard Alert

Siren

---

# Network Infrastructure

Factory Network

Office Network

Guest Network

IoT Network

Server Network

VPN

Firewall

---

# Hardware Security

Every hardware device has

Unique ID

Authentication

Encrypted Communication

Access Control

Audit Log

Heartbeat Signal

Firmware Version

Status

---

# Device Heartbeat

Every device sends heartbeat

Every 30 Seconds

If heartbeat missing

↓

Warning

↓

Critical

↓

Maintenance Ticket

---

# Hardware Status

Online

Offline

Maintenance

Updating

Unknown

Alarm

---

# Digital Twin

Every hardware device has

Digital Representation

Real-Time Status

Historical Data

AI Prediction

Health Score

Maintenance Score

Energy Score

---

# Hardware Business Rules

## HW-BR-001

Every hardware device must have a unique ID.

## HW-BR-002

All hardware communication must be encrypted where supported.

## HW-BR-003

Machine production cannot continue after Emergency Stop until authorized reset.

## HW-BR-004

Every hardware event must generate an audit record.

## HW-BR-005

Temperature and pressure alarms must immediately notify responsible personnel.

## HW-BR-006

Every QR scan must be validated before inventory movement.

## HW-BR-007

Hardware failures must automatically create maintenance requests.

## HW-BR-008

Energy meters must store historical data for long-term analysis.

## HW-BR-009

AI recommendations never directly control hardware without human approval.

## HW-BR-010

All hardware must synchronize timestamps with the central server.

---

# Hardware Integration Status

Status: Approved Draft

Version: 1.0
---

# Industrial IoT Architecture

## Purpose

The Industrial IoT layer connects every physical asset inside the factory to FIXAR OS.

Every sensor, PLC, machine, scale, camera and industrial device continuously transmits operational data to the central platform.

---

# IoT Architecture

Physical Device

↓

PLC / Controller

↓

IoT Gateway

↓

Message Broker

↓

FIXAR API

↓

Database

↓

AI Engine

↓

Dashboard

↓

Digital Twin

---

# IoT Gateway

Gateway Responsibilities

- Collect PLC Data
- Convert Protocols
- Buffer Offline Data
- Compress Messages
- Encrypt Communication
- Synchronize Time
- Health Monitoring

Supported Platforms

- Siemens IOT2050
- Advantech Gateway
- Moxa Gateway
- Industrial Raspberry Pi
- Beckhoff IPC

---

# MQTT Architecture

Topics

factory/status

factory/alarms

machine/status

machine/cycle

machine/temperature

machine/pressure

machine/power

production/events

inventory/events

warehouse/scans

quality/events

maintenance/events

ai/events

---

# OPC-UA Tags

Machine.Status

Machine.CycleCount

Machine.CycleTime

Machine.Recipe

Machine.Operator

Machine.Product

Machine.Temperature

Machine.Pressure

Machine.Alarm

Machine.Power

Machine.Energy

Machine.Mold

Machine.Station

Machine.WorkOrder

Machine.Lot

---

# Event Processing

Machine Event

↓

Validation

↓

Business Rules

↓

Database

↓

Dashboard

↓

AI Analysis

↓

Notification

↓

Digital Twin

---

# Event Types

Machine Started

Machine Stopped

Cycle Completed

Alarm Raised

Alarm Cleared

Maintenance Started

Maintenance Finished

Recipe Changed

Operator Changed

Shift Changed

Power Failure

Emergency Stop

QR Scan

Inventory Movement

Shipment Loaded

---

# Digital Inputs

Emergency Stop

Safety Door

Start Button

Stop Button

Reset Button

Limit Switch

Pressure Switch

Level Switch

---

# Digital Outputs

Start Machine

Stop Machine

Alarm Light

Tower Lamp

Buzzer

Cooling Fan

Signal Lamp

---

# Analog Inputs

Temperature

Pressure

Humidity

Current

Voltage

Frequency

Power

Energy

Flow

Weight

---

# Sampling Rates

Machine Status

Every 1 Second

Production Counter

Every Cycle

Temperature

Every 5 Seconds

Pressure

Every 5 Seconds

Energy

Every 30 Seconds

Warehouse Sensors

Every 30 Seconds

Environment

Every Minute

---

# Data Buffering

If communication is lost

↓

Gateway stores locally

↓

Connection restored

↓

Automatic synchronization

↓

Audit Log generated

---

# Offline Mode

Production continues.

All events are buffered.

Synchronization begins automatically after reconnecting.

No production data may be lost.

---

# Network Topology

Internet

↓

Firewall

↓

Core Switch

↓

Application Server

↓

Database Server

↓

AI Server

↓

Backup Server

↓

Industrial Switch

↓

PLC Network

↓

Machines

↓

Sensors

---

# VLAN Structure

VLAN 10

Office

VLAN 20

Production

VLAN 30

Warehouse

VLAN 40

IoT

VLAN 50

Servers

VLAN 60

Guest

---

# Edge Computing

Edge devices perform

Filtering

Aggregation

Protocol Conversion

Data Compression

Temporary Storage

Health Monitoring

Alarm Detection

---

# Synchronization Rules

Server Time

↓

Gateway Time

↓

PLC Time

↓

Sensor Time

Maximum allowed difference

100 milliseconds

---

# Time Synchronization

Protocol

NTP

Backup

GPS Time

---

# Device Registration

Every device requires

Device ID

Device Name

Hardware Type

Firmware Version

MAC Address

IP Address

Location

Responsible Department

Installation Date

Status

---

# Device Lifecycle

Registered

↓

Configured

↓

Commissioned

↓

Operational

↓

Maintenance

↓

Calibration

↓

Retired

---

# Device Health Score

Calculated From

Communication Quality

Error Count

Temperature

Power Stability

Firmware Status

Maintenance History

Sensor Reliability

---

# Predictive Maintenance Inputs

Cycle Count

Bearing Temperature

Motor Current

Motor Voltage

Vibration

Oil Temperature

Oil Pressure

Alarm Frequency

Downtime History

Repair History

---

# Smart Alerts

Machine Overheating

↓

AI Analysis

↓

Maintenance Prediction

↓

Responsible Technician

↓

Notification

↓

Dashboard

↓

Maintenance Work Order

---

# Hardware Audit Trail

Every hardware interaction records

Timestamp

User

Machine

Station

Sensor

Previous Value

New Value

Location

IP Address

Gateway

PLC

---

# PLC Redundancy

Primary PLC

↓

Heartbeat

↓

Backup PLC

↓

Automatic Failover

↓

Operator Notification

---

# Server Infrastructure

Application Server

API Server

AI Server

Database Server

Message Queue

Backup Server

Monitoring Server

Logging Server

---

# Recommended Technologies

Backend

ASP.NET Core

Database

PostgreSQL

Realtime

SignalR

MQTT Broker

EMQX

Message Queue

RabbitMQ

Industrial Communication

OPC-UA

Cache

Redis

Object Storage

MinIO

Monitoring

Prometheus

Visualization

Grafana

Container

Docker

Container Orchestration

Kubernetes

---

# Cyber Security

Zero Trust

Role Based Access

Encrypted Communication

VPN

Multi Factor Authentication

Certificate Authentication

Firewall

IDS

IPS

Network Segmentation

Audit Logging

---

# Disaster Recovery

Automatic Backup

Redundant Database

Redundant API

Redundant AI Server

Automatic Failover

Recovery Verification

Business Continuity

---

# Smart Factory Maturity

Level 1

Connected Machines

Level 2

Connected Production

Level 3

Connected Warehouse

Level 4

Real-Time Factory

Level 5

AI Driven Smart Factory

Level 6

Autonomous Decision Support

---

# Hardware KPIs

Machine Availability

Machine Utilization

OEE

MTBF

MTTR

Energy Per Pair

Air Consumption

Material Consumption

Temperature Stability

Pressure Stability

Cycle Stability

Operator Productivity

Warehouse Accuracy

QR Accuracy

Inventory Accuracy

Alarm Frequency

---

# Hardware Integration Completion

Status

Approved

Version

1.0

---
---

# FIXAR Production Line Integration

## Purpose

This section defines the complete integration of FIXAR polyurethane insole production lines with FIXAR OS.

The objective is to capture every production event automatically without manual data entry whenever possible.

---

# Production Line

Current Configuration

Production Technology

Polyurethane Injection

Stations

24 Stations

Operation

Rotary Carousel

Products

Memory Foam Insoles

Polyurethane Insoles

Orthopedic Insoles

OEM Insoles

---

# Production Flow

Customer Order

↓

Production Planning

↓

Recipe Selection

↓

Raw Material Verification

↓

Machine Setup

↓

Mold Installation

↓

Production Start

↓

Injection

↓

Foaming

↓

Curing

↓

Demolding

↓

Inspection

↓

Cutting

↓

DTF Printing

↓

Packaging

↓

Warehouse

↓

Shipment

---

# Machine Startup Checklist

Before production begins the system verifies

Approved Recipe

Approved Mold

Machine Temperature

Tank Temperature

Raw Material Availability

Operator Authorization

Shift Information

Maintenance Status

Emergency Stop Status

Previous Alarm Clearance

Power Availability

Compressed Air

---

# Automatic Recipe Verification

Machine requests

Recipe Version

↓

FIXAR OS

↓

Recipe Validation

↓

PLC Confirmation

↓

Production Enabled

---

# Recipe Parameters

Every production lot stores

Recipe Version

Polyol Type

Isocyanate Type

Crosslinker

Pigment

Mix Ratio

Target Density

Target Hardness

Mix Time

Injection Time

Curing Time

---

# Mold Verification

Operator scans mold QR.

System verifies

Correct Product

Correct Size

Maintenance Status

Production Counter

Lifetime

Approval Status

Cleaning Status

---

# Mold Change Workflow

Scan Old Mold

↓

Remove Mold

↓

Install New Mold

↓

Scan QR

↓

Validation

↓

Approval

↓

Production Enabled

---

# Operator Identification

Every operator logs in using

QR Card

RFID Card

PIN

Biometric

Every production cycle stores

Operator ID

Shift

Machine

Station

Timestamp

---

# Automatic Cycle Collection

Each completed cycle records

Cycle Number

Cycle Time

Machine

Station

Operator

Recipe

Mold

Temperature

Pressure

Produced Quantity

Scrap Quantity

---

# Production Counter

Machine

↓

PLC Counter

↓

Gateway

↓

FIXAR OS

↓

Database

↓

Dashboard

↓

AI

---

# Scrap Recording

Scrap Types

Incomplete Foam

Air Bubble

Density Error

Wrong Hardness

Surface Damage

Fabric Error

Color Difference

Contamination

Cutting Damage

Packaging Damage

Each scrap record stores

Reason

Quantity

Operator

Machine

Photo

Timestamp

Corrective Action

---

# Material Consumption

For every production lot

System automatically calculates

Polyol

Isocyanate

Crosslinker

Pigment

Fabric

Packaging

Labels

Boxes

---

# Material Traceability

Every pair can trace

↓

Polyol Barrel

↓

Supplier

↓

Supplier Batch

↓

Receiving Date

↓

Quality Test

↓

Storage Location

↓

Production Lot

↓

Customer

---

# Station Dashboard

Every station displays

Current Product

Current Recipe

Current Mold

Operator

Cycle Counter

Today's Production

Today's Scrap

Alarm Status

Remaining Work Order

---

# Production Dashboard

Shows

24 Stations

Live Status

Cycle Timer

Running Count

Idle Count

Alarm Count

Expected Finish Time

Current OEE

---

# Production Alarms

Machine Offline

Emergency Stop

Temperature High

Temperature Low

Pressure High

Pressure Low

Cycle Timeout

Recipe Error

Wrong Mold

Wrong Material

Communication Failure

---

# AI Production Monitoring

AI continuously analyzes

Cycle Stability

Temperature Stability

Pressure Stability

Scrap Trend

Operator Performance

Machine Efficiency

Material Consumption

Energy Consumption

---

# AI Recommendations

Reduce Cycle Time

Increase Temperature

Decrease Pressure

Schedule Maintenance

Replace Mold

Inspect Material

Train Operator

Optimize Recipe

---

# Production KPIs

Pairs Per Hour

Pairs Per Shift

Pairs Per Day

Scrap %

OEE

Availability

Performance

Quality

Cycle Stability

Energy Per Pair

Material Per Pair

Operator Productivity

---

# Shift Management

Morning Shift

Evening Shift

Night Shift

Every shift records

Supervisor

Operators

Machine Status

Production

Scrap

Downtime

Maintenance

Comments

---

# Downtime Reasons

Material Waiting

Machine Failure

Operator Break

Power Failure

Maintenance

Cleaning

Recipe Change

Mold Change

Quality Hold

Unknown

---

# Downtime Workflow

Machine Stops

↓

PLC Detects

↓

FIXAR OS Creates Event

↓

Operator Selects Reason

↓

Timer Starts

↓

Machine Restarts

↓

Downtime Saved

↓

AI Analysis

---

# Production Reports

Hourly Production

Shift Production

Daily Production

Weekly Production

Monthly Production

Machine Comparison

Operator Comparison

Recipe Comparison

Customer Production

Product Production

---

# Executive Dashboard

Shows

Factory Status

Production Progress

Delayed Orders

Machine Health

Material Risk

Quality Trend

Shipment Readiness

Financial Impact

AI Executive Summary

---

# FIXAR Hardware Business Rules

## HW-FX-001

Production cannot start without an approved recipe.

## HW-FX-002

Production cannot start if the wrong mold is installed.

## HW-FX-003

Every production lot must reference the actual operator.

## HW-FX-004

Every completed cycle increments production counters automatically.

## HW-FX-005

Emergency Stop events immediately suspend production recording.

## HW-FX-006

Every material lot used in production must be traceable.

## HW-FX-007

Every production alarm generates an audit log and notification.

## HW-FX-008

AI recommendations require supervisor approval before operational changes.

## HW-FX-009

No production record may be deleted after completion.

## HW-FX-010

All production timestamps must originate from synchronized system time.

---

# Industry 4.0 Compliance

FIXAR OS supports

Digital Manufacturing

Industrial IoT

Smart Factory

Digital Twin

Predictive Maintenance

AI Decision Support

Paperless Production

End-to-End Traceability

Real-Time Analytics

Continuous Improvement

---

# Hardware Integration Status

Status: Production Integration Complete

Version: 1.1

---
