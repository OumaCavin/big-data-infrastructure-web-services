# Shopping Receipt Web Service - User Manual

**Author**: Cavin Otieno  
**Course**: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing  
**Date**: 2025-11-27  

## Overview

The Shopping Receipt Web Service demonstrates web services technologies including:

- RESTful API for receipt management
- XML and JSON data formats
- Orchestration vs Choreography patterns
- Web service validation and testing
- Big Data infrastructure integration

## Quick Start

### 1. Start the Service
```bash
cd iis_dotnet/ShoppingReceiptService
dotnet run
```

### 2. Access Swagger UI
Open http://localhost:5000 in your browser

### 3. Test Basic Endpoints
```bash
# Get all receipts
curl http://localhost:5000/api/receipt

# Get specific receipt
curl http://localhost:5000/api/receipt/RCPT_2025_001

# Check health
curl http://localhost:5000/health
```

## API Endpoints

- `GET /api/receipt` - Get all receipts
- `GET /api/receipt/{id}` - Get receipt by ID
- `POST /api/receipt/validate` - Validate receipt data
- `GET /api/receipt/search` - Search receipts
- `GET /api/receipt/statistics` - Get statistics
- `GET /api/receipt/{id}/export` - Export receipt

## Pattern Implementation

### Orchestration
- Central controller coordinates services
- Sequential processing workflow
- Centralized error handling

### Choreography  
- Event-driven service interaction
- Decentralized coordination
- Event bus messaging

## Data Formats

Both XML and JSON formats are supported with schema validation.

## Testing

Use the provided validation scripts:
- `xml_receipt/xml_validator.py`
- `json_receipt/json_validator.py`