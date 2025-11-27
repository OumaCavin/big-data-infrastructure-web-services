#!/usr/bin/env python3
"""
XML Receipt Validator for Shopping Receipt System
Validates XML receipt against XSD schema and business rules
"""

import sys
import json
from datetime import datetime
from typing import Dict, List, Tuple, Any

# Try to import lxml for XSD validation
try:
    from lxml import etree
    LXML_AVAILABLE = True
except ImportError:
    print("Warning: lxml not available. Install with: pip install lxml")
    LXML_AVAILABLE = False

class XMLReceiptValidator:
    """Validates XML shopping receipt against XSD schema and business rules"""
    
    def __init__(self, xml_file: str, xsd_file: str):
        self.xml_file = xml_file
        self.xsd_file = xsd_file
        self.errors = []
        self.warnings = []
        
    def validate_schema(self) -> bool:
        """Validate XML against XSD schema"""
        if not LXML_AVAILABLE:
            self.errors.append("XSD validation requires lxml library")
            return False
            
        try:
            # Parse XSD schema
            with open(self.xsd_file, 'r', encoding='utf-8') as f:
                schema_root = etree.parse(f)
            schema = etree.XMLSchema(schema_root)
            
            # Parse XML document
            xml_doc = etree.parse(self.xml_file)
            
            # Validate
            if schema.validate(xml_doc):
                return True
            else:
                for error in schema.error_log:
                    self.errors.append(f"Schema validation error: {error}")
                return False
                
        except Exception as e:
            self.errors.append(f"Schema validation failed: {str(e)}")
            return False
    
    def validate_business_rules(self, xml_data: Dict) -> bool:
        """Validate business rules for the receipt"""
        is_valid = True
        
        # Rule 1: Minimum 5 items required
        items_count = len(xml_data.get('items', {}).get('item', []))
        if items_count < 5:
            self.errors.append(f"Receipt must contain at least 5 items. Found: {items_count}")
            is_valid = False
        else:
            self.warnings.append(f"Item count validation passed: {items_count} items")
        
        # Rule 2: Date validation (purchase date should not be in future)
        purchase_date = xml_data.get('purchase_date')
        if purchase_date:
            try:
                purchase_dt = datetime.strptime(purchase_date, '%Y-%m-%d')
                if purchase_dt > datetime.now():
                    self.errors.append("Purchase date cannot be in the future")
                    is_valid = False
            except ValueError:
                self.errors.append("Invalid date format. Expected YYYY-MM-DD")
                is_valid = False
        
        # Rule 3: Total amount validation
        total_amount = float(xml_data.get('total_amount', 0))
        items_pricing = xml_data.get('pricing', {})
        
        if 'grand_total' in items_pricing:
            calculated_total = float(items_pricing['grand_total'])
            if abs(total_amount - calculated_total) > 0.01:  # Allow small rounding differences
                self.errors.append(f"Total amount mismatch. Header: {total_amount}, Pricing: {calculated_total}")
                is_valid = False
        
        # Rule 4: Item validation
        items = xml_data.get('items', {}).get('item', [])
        for i, item in enumerate(items):
            quantity = int(item.get('quantity', 0))
            unit_price = float(item.get('unit_price', 0))
            subtotal = float(item.get('subtotal', 0))
            
            # Check if quantity is positive
            if quantity <= 0:
                self.errors.append(f"Item {i+1}: Quantity must be positive. Found: {quantity}")
                is_valid = False
            
            # Check if unit price is positive
            if unit_price < 0:
                self.errors.append(f"Item {i+1}: Unit price cannot be negative. Found: {unit_price}")
                is_valid = False
            
            # Check subtotal calculation
            expected_subtotal = quantity * unit_price
            if abs(subtotal - expected_subtotal) > 0.01:
                self.errors.append(f"Item {i+1}: Subtotal calculation error. Expected: {expected_subtotal}, Found: {subtotal}")
                is_valid = False
        
        # Rule 5: Customer information validation
        customer = xml_data.get('customer', {})
        if customer.get('loyalty_member'):
            if not customer.get('member_since'):
                self.warnings.append("Loyalty member should have member_since date")
        
        # Rule 6: Payment method validation
        payment_method = xml_data.get('payment_method')
        payment = xml_data.get('payment', {})
        
        if payment_method == 'Credit Card':
            if not payment.get('card_type') or not payment.get('card_number_last_four'):
                self.errors.append("Credit Card payment requires card_type and card_number_last_four")
                is_valid = False
        
        # Rule 7: Digital signature validation
        digital_sig = xml_data.get('digital_signature', {})
        if not digital_sig.get('algorithm') or not digital_sig.get('signature'):
            self.errors.append("Digital signature must include algorithm and signature")
            is_valid = False
        
        return is_valid
    
    def validate_xml_content(self) -> bool:
        """Validate XML file content and structure"""
        try:
            if not LXML_AVAILABLE:
                # Fallback to basic XML parsing
                import xml.etree.ElementTree as ET
                
                try:
                    tree = ET.parse(self.xml_file)
                    root = tree.getroot()
                    
                    # Convert to dict for business rule validation
                    def xml_to_dict(element):
                        """Convert XML element to dictionary"""
                        result = {}
                        
                        # Add attributes
                        for key, value in element.attrib.items():
                            result[f"@{key}"] = value
                        
                        # Add text content
                        if element.text and element.text.strip():
                            if len(element) == 0:  # No child elements
                                return element.text.strip()
                            else:
                                result['#text'] = element.text.strip()
                        
                        # Add child elements
                        for child in element:
                            child_data = xml_to_dict(child)
                            if child.tag in result:
                                # Handle multiple elements with same tag
                                if not isinstance(result[child.tag], list):
                                    result[child.tag] = [result[child.tag]]
                                result[child.tag].append(child_data)
                            else:
                                result[child.tag] = child_data
                        
                        return result
                    
                    xml_data = xml_to_dict(root)
                    
                except ET.ParseError as e:
                    self.errors.append(f"XML parsing error: {str(e)}")
                    return False
                    
            else:
                # Use lxml for better parsing
                tree = etree.parse(self.xml_file)
                root = tree.getroot()
                
                # Convert lxml element to dict
                def lxml_to_dict(element):
                    """Convert lxml element to dictionary"""
                    result = {}
                    
                    # Add attributes
                    for key, value in element.attrib.items():
                        result[f"@{key}"] = value
                    
                    # Add text content
                    if element.text and element.text.strip():
                        if len(element) == 0:  # No child elements
                            return element.text.strip()
                        else:
                            result['#text'] = element.text.strip()
                    
                    # Add child elements
                    for child in element:
                        child_data = lxml_to_dict(child)
                        if child.tag in result:
                            # Handle multiple elements with same tag
                            if not isinstance(result[child.tag], list):
                                result[child.tag] = [result[child.tag]]
                            result[child.tag].append(child_data)
                        else:
                            result[child.tag] = child_data
                    
                    return result
                
                xml_data = lxml_to_dict(root)
            
            # Validate business rules
            return self.validate_business_rules(xml_data)
            
        except Exception as e:
            self.errors.append(f"XML content validation failed: {str(e)}")
            return False
    
    def generate_report(self) -> Dict[str, Any]:
        """Generate validation report"""
        return {
            'file': self.xml_file,
            'timestamp': datetime.now().isoformat(),
            'schema_valid': self.validate_schema() if LXML_AVAILABLE else None,
            'content_valid': self.validate_xml_content(),
            'errors': self.errors,
            'warnings': self.warnings,
            'overall_valid': len(self.errors) == 0
        }
    
    def print_report(self):
        """Print validation report to console"""
        report = self.generate_report()
        
        print("=" * 60)
        print("XML RECEIPT VALIDATION REPORT")
        print("=" * 60)
        print(f"File: {report['file']}")
        print(f"Timestamp: {report['timestamp']}")
        print(f"Overall Status: {'✓ VALID' if report['overall_valid'] else '✗ INVALID'}")
        print()
        
        if LXML_AVAILABLE:
            print(f"Schema Validation: {'✓ PASSED' if report['schema_valid'] else '✗ FAILED'}")
        
        print(f"Content Validation: {'✓ PASSED' if report['content_valid'] else '✗ FAILED'}")
        print()
        
        if report['errors']:
            print("ERRORS:")
            for i, error in enumerate(report['errors'], 1):
                print(f"  {i}. {error}")
            print()
        
        if report['warnings']:
            print("WARNINGS:")
            for i, warning in enumerate(report['warnings'], 1):
                print(f"  {i}. {warning}")
            print()
        
        print("=" * 60)
        
        return report['overall_valid']

def main():
    """Main validation function"""
    import argparse
    
    parser = argparse.ArgumentParser(description='Validate XML shopping receipt')
    parser.add_argument('xml_file', help='Path to XML receipt file')
    parser.add_argument('--xsd', default='receipt_schema.xsd', 
                       help='Path to XSD schema file (default: receipt_schema.xsd)')
    parser.add_argument('--json-report', help='Save report to JSON file')
    parser.add_argument('--quiet', action='store_true', help='Suppress output')
    
    args = parser.parse_args()
    
    # Create validator
    validator = XMLReceiptValidator(args.xml_file, args.xsd)
    
    # Run validation
    is_valid = validator.print_report()
    
    # Save JSON report if requested
    if args.json_report:
        report = validator.generate_report()
        with open(args.json_report, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2)
        print(f"Report saved to: {args.json_report}")
    
    # Exit with appropriate code
    sys.exit(0 if is_valid else 1)

if __name__ == "__main__":
    # Default validation when run directly
    if len(sys.argv) == 1:
        # Default files for testing
        xml_file = "shopping_receipt.xml"
        xsd_file = "receipt_schema.xsd"
        
        validator = XMLReceiptValidator(xml_file, xsd_file)
        validator.print_report()
    else:
        main()