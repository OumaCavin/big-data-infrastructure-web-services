# SDS 6104 - Big Data Infrastructure, Platforms and Warehousing
## Homework Submission - Web Services Implementation

**Author**: Cavin Otieno  
**Course**: SDS 6104  
**Date**: 2025-11-27  
**Due Date**: December 5, 2025  

---

## 📋 Project Overview

This project implements a comprehensive Web Services solution covering:

1. **Hadoop Installation & Setup**
2. **XML Shopping Receipt System** (5+ items)
3. **JSON Shopping Receipt System**
4. **Microsoft IIS Web Server with ASP.NET Web Service**
5. **Web Service Choreography & Orchestration**

---

## 📁 Project Structure

```
big-data-infrastructure-web-services/
├── README.md                           # This file
├── hadoop_installation/                # Hadoop setup files
│   ├── installation_guide.md
│   ├── hadoop_setup.py
│   └── hadoop_configs/
├── xml_receipt/                        # XML shopping receipt
│   ├── shopping_receipt.xml
│   ├── receipt_schema.xsd
│   └── xml_validator.py
├── json_receipt/                       # JSON shopping receipt
│   ├── shopping_receipt.json
│   └── json_validator.py
├── iis_dotnet/                         # ASP.NET Web Service
│   ├── ShoppingReceiptService/
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── Properties/
│   └── deployment_guide.md
├── choreography_orchestration/         # Web service patterns
│   ├── orchestration_example.cs
│   ├── choreography_example.cs
│   ├── bpel_example.xml
│   └── comparison_analysis.md
└── documentation/                      # Additional documentation
    ├── user_manual.md
    └── api_documentation.md
```

---

## 🚀 Quick Start

### Prerequisites
- .NET 6.0+ SDK
- Java 8+ (for Hadoop)
- IIS (Windows) or Apache/Nginx (Linux)
- Python 3.8+ (for validation scripts)

### Installation Steps

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd big-data-infrastructure-web-services
   ```

2. **Setup Hadoop**
   ```bash
   cd hadoop_installation
   python hadoop_setup.py
   ```

3. **Validate XML/JSON Receipts**
   ```bash
   cd xml_receipt
   python xml_validator.py
   
   cd ../json_receipt
   python json_validator.py
   ```

4. **Run ASP.NET Web Service**
   ```bash
   cd iis_dotnet/ShoppingReceiptService
   dotnet run
   ```

---

## 📊 Web Service Implementation Details

### XML Shopping Receipt
- **5+ Items**: Coffee beans, bread, yogurt, bananas, chicken breast
- **Complete Customer Data**: ID, name, contact information
- **Store Information**: Address, tax ID, cashier details
- **Payment Details**: Credit card information, authorization codes
- **XSD Validation**: Complete schema for structure validation

### JSON Shopping Receipt
- **Same Data Structure**: Equivalent JSON representation
- **Type Validation**: Proper data types and validation
- **API Integration**: Ready for REST API consumption

### ASP.NET Web Service
- **RESTful API**: Clean API design following REST principles
- **Receipt Management**: Get receipt by ID, validate receipt structure
- **Cross-platform**: Runs on Windows IIS or Linux/Apache
- **Documentation**: Swagger/OpenAPI integration

### Choreography vs Orchestration
- **Orchestration Pattern**: Centralized control with OrderOrchestrator
- **Choreography Pattern**: Event-driven distributed architecture
- **BPEL Implementation**: Business Process Execution Language example
- **Performance Analysis**: Comparison of both approaches

---

## 🔧 Technical Specifications

### Hadoop Configuration
- **Version**: 3.3.6
- **Mode**: Single-node (pseudo-distributed)
- **Storage**: HDFS with replication factor 1
- **Processing**: YARN-based MapReduce

### Web Service Stack
- **Framework**: ASP.NET Core 6.0
- **API Style**: RESTful JSON/XML
- **Validation**: XML Schema (XSD) + JSON Schema
- **Documentation**: Swagger/OpenAPI 3.0

### Data Formats
- **XML**: W3C compliant with proper namespaces
- **JSON**: RFC 8259 compliant
- **Validation**: Automated scripts with comprehensive checks

---

## 📈 Key Learning Outcomes

1. **Service-Oriented Architecture**: Understanding of SOA principles
2. **Web Services Standards**: SOAP, WSDL, UDDI implementation
3. **Data Interchange**: XML/JSON mastery for web services
4. **Service Patterns**: Orchestration vs Choreography implementation
5. **Big Data Infrastructure**: Hadoop ecosystem setup and usage
6. **Enterprise Integration**: IIS + ASP.NET service deployment

---

## 🎯 Assignment Requirements Met

- ✅ **Hadoop Installation**: Complete setup guide and scripts
- ✅ **XML Receipt (5+ items)**: Detailed shopping receipt with validation
- ✅ **JSON Receipt**: Equivalent JSON implementation
- ✅ **IIS Web Server**: ASP.NET Core service for Windows IIS
- ✅ **Choreography & Orchestration**: Both patterns with examples

---

## 📚 Related Lecture Concepts

### Web Services Characteristics (from Lecture 3)
- **Loose Coupling**: Services can interact independently
- **Service Granularity**: Atomic vs composite services
- **Synchronicity**: Synchronous and asynchronous patterns
- **Well-definedness**: WSDL and service contracts

### Technology Stack
- **Enabling**: HTTP, XML
- **Core Services**: SOAP, WSDL, UDDI
- **Composition**: WS-CDL, BPEL, WS-Security

### Quality of Service (QoS)
- **Availability**: Service uptime and reliability
- **Performance**: Response time and throughput
- **Security**: Authentication and authorization
- **Scalability**: Handling varying loads

---

## 🔍 Testing & Validation

Each component includes:
- **Unit Tests**: Automated validation scripts
- **Integration Tests**: Service-to-service communication
- **Performance Tests**: Load and stress testing
- **Documentation**: Complete API and usage guides

---

## 📞 Support & Contact

For questions or issues with this implementation:
- Check the individual component README files
- Review the API documentation
- Consult the troubleshooting guides

---

## 📝 License

This project is created for educational purposes as part of SDS 6104 coursework.

---

**Last Updated**: 2025-11-27  
**Version**: 1.0  
**Status**: Complete Implementation