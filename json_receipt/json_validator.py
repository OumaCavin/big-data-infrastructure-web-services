#!/usr/bin/env python3
"""
JSON Receipt Validator for Shopping Receipt System
Validates JSON receipt against JSON schema and business rules
"""

import sys
import json
from datetime import datetime
from typing import Dict, List, Tuple, Any

# Try to import jsonschema for JSON Schema validation
try:
    import jsonschema
    from jsonschema import validate, ValidationError, Draft7Validator
    JSONSCHEMA_AVAILABLE = True
except ImportError:
    print("Warning: jsonschema not available. Install with: pip install jsonschema")
    JSONSCHEMA_AVAILABLE = False

class JSONReceiptValidator:
    """Validates JSON shopping receipt against JSON schema and business rules"""
    
    def __init__(self, json_file: str, schema_file: str = None):
        self.json_file = json_file
        self.schema_file = schema_file
        self.errors = []
        self.warnings = []
        self.json_data = None
        
    def load_json(self) -> bool:
        """Load and parse JSON file"""
        try:
            with open(self.json_file, 'r', encoding='utf-8') as f:
                self.json_data = json.load(f)
            return True
        except json.JSONDecodeError as e:
            self.errors.append(f"JSON parsing error: {str(e)}")
            return False
        except Exception as e:
            self.errors.append(f"Error loading JSON file: {str(e)}")
            return False
    
    def validate_schema(self) -> bool:
        """Validate JSON against JSON Schema"""
        if not JSONSCHEMA_AVAILABLE:
            self.errors.append("JSON Schema validation requires jsonschema library")
            return False
            
        if not self.json_data:
            self.errors.append("No JSON data loaded for schema validation")
            return False
        
        try:
            with open(self.schema_file, 'r', encoding='utf-8') as f:
                schema = json.load(f)
            
            validate(instance=self.json_data, schema=schema)
            return True
            
        except ValidationError as e:
            self.errors.append(f"Schema validation error: {e.message}")
            if e.path:
                self.errors.append(f"  At path: {' -> '.join(str(p) for p in e.path)}")
            return False
        except Exception as e:
            self.errors.append(f"Schema validation failed: {str(e)}")
            return False
    
    def validate_business_rules(self) -> bool:
        """Validate business rules for the receipt"""
        if not self.json_data:
            self.errors.append("No JSON data loaded for business rule validation")
            return False
        
        is_valid = True
        
        # Rule 1: Minimum 5 items required
        items = self.json_data.get('items', [])
        items_count = len(items)
        if items_count < 5:
            self.errors.append(f"Receipt must contain at least 5 items. Found: {items_count}")
            is_valid = False
        else:
            self.warnings.append(f"Item count validation passed: {items_count} items")
        
        # Rule 2: Date validation (purchase date should not be in future)
        purchase_date = self.json_data.get('purchase_date')
        if purchase_date:
            try:
                purchase_dt = datetime.fromisoformat(purchase_date.replace('Z', '+00:00'))
                if purchase_dt > datetime.now().replace(tzinfo=purchase_dt.tzinfo):
                    self.errors.append("Purchase date cannot be in the future")
                    is_valid = False
            except ValueError as e:
                self.errors.append(f"Invalid date format: {str(e)}")
                is_valid = False
        
        # Rule 3: Total amount validation
        total_amount = float(self.json_data.get('total_amount', 0))
        pricing = self.json_data.get('pricing', {})
        
        if 'grand_total' in pricing:
            calculated_total = float(pricing['grand_total'])
            if abs(total_amount - calculated_total) > 0.01:  # Allow small rounding differences
                self.errors.append(f"Total amount mismatch. Header: {total_amount}, Pricing: {calculated_total}")
                is_valid = False
        
        # Rule 4: Item validation
        for i, item in enumerate(items):
            quantity = item.get('quantity', 0)
            unit_price = item.get('unit_price', 0)
            subtotal = item.get('subtotal', 0)
            
            # Check if quantity is positive integer
            if not isinstance(quantity, int) or quantity <= 0:
                self.errors.append(f"Item {i+1}: Quantity must be positive integer. Found: {quantity}")
                is_valid = False
            
            # Check if unit price is non-negative
            if not isinstance(unit_price, (int, float)) or unit_price < 0:
                self.errors.append(f"Item {i+1}: Unit price cannot be negative. Found: {unit_price}")
                is_valid = False
            
            # Check subtotal calculation
            expected_subtotal = quantity * unit_price
            if abs(subtotal - expected_subtotal) > 0.01:
                self.errors.append(f"Item {i+1}: Subtotal calculation error. Expected: {expected_subtotal}, Found: {subtotal}")
                is_valid = False
        
        # Rule 5: Customer validation
        customer = self.json_data.get('customer', {})
        if customer.get('loyalty_member'):
            if not customer.get('member_since'):
                self.warnings.append("Loyalty member should have member_since date")
        
        # Rule 6: Email format validation
        email = customer.get('email', '')
        if email and '@' not in email:
            self.errors.append(f"Invalid email format: {email}")
            is_valid = False
        
        # Rule 7: Payment method validation
        payment_method = self.json_data.get('payment_method')
        payment = self.json_data.get('payment', {})
        
        if payment_method == 'Credit Card':
            if not payment.get('card_type') or not payment.get('card_number_last_four'):
                self.errors.append("Credit Card payment requires card_type and card_number_last_four")
                is_valid = False
            
            # Validate card number format
            card_number = payment.get('card_number_last_four', '')
            if card_number and not card_number.isdigit() or len(card_number) != 4:
                self.errors.append(f"Card number last four must be 4 digits. Found: {card_number}")
                is_valid = False
        
        # Rule 8: Digital signature validation
        digital_sig = self.json_data.get('digital_signature', {})
        if not digital_sig.get('algorithm') or not digital_sig.get('signature'):
            self.errors.append("Digital signature must include algorithm and signature")
            is_valid = False
        
        # Rule 9: Currency consistency
        prices = []
        for item in items:
            prices.extend([item.get('unit_price', 0), item.get('subtotal', 0)])
        
        pricing_section = self.json_data.get('pricing', {})
        if 'subtotal' in pricing_section:
            prices.append(pricing_section['subtotal'])
        if 'grand_total' in pricing_section:
            prices.append(pricing_section['grand_total'])
        
        # Check for reasonable price ranges (not too large, not negative)
        for price in prices:
            if price < 0:
                self.errors.append(f"Price cannot be negative: {price}")
                is_valid = False
            elif price > 100000:  # Very expensive items (could be error)
                self.warnings.append(f"Unusually high price: {price}")
        
        # Rule 10: Required fields presence
        required_fields = [
            'receipt_id', 'purchase_date', 'store_name', 'customer', 'store',
            'items', 'payment', 'transaction', 'footer'
        ]
        
        for field in required_fields:
            if field not in self.json_data:
                self.errors.append(f"Required field missing: {field}")
                is_valid = False
        
        return is_valid
    
    def validate_data_types(self) -> bool:
        """Validate data types for key fields"""
        if not self.json_data:
            self.errors.append("No JSON data loaded for data type validation")
            return False
        
        is_valid = True
        
        # Check receipt_id format
        receipt_id = self.json_data.get('receipt_id', '')
        if not isinstance(receipt_id, str) or not receipt_id.startswith('RCPT_'):
            self.warnings.append(f"Receipt ID format may be incorrect: {receipt_id}")
        
        # Check numeric fields
        numeric_fields = ['total_amount']
        for field in numeric_fields:
            value = self.json_data.get(field)
            if value is not None and not isinstance(value, (int, float)):
                self.errors.append(f"Field '{field}' must be numeric. Found: {type(value).__name__}")
                is_valid = False
        
        # Check date fields
        date_fields = ['purchase_date']
        for field in date_fields:
            value = self.json_data.get(field)
            if value is not None:
                try:
                    datetime.fromisoformat(value.replace('Z', '+00:00'))
                except ValueError:
                    self.errors.append(f"Field '{field}' must be valid ISO date. Found: {value}")
                    is_valid = False
        
        return is_valid
    
    def calculate_totals(self) -> Dict[str, float]:
        """Calculate expected totals from items"""
        items = self.json_data.get('items', [])
        
        subtotal = sum(item.get('subtotal', 0) for item in items)
        taxable_subtotal = sum(
            item.get('subtotal', 0) 
            for item in items 
            if item.get('tax_applied', False)
        )
        
        return {
            'calculated_subtotal': subtotal,
            'taxable_subtotal': taxable_subtotal,
            'item_count': len(items)
        }
    
    def generate_report(self) -> Dict[str, Any]:
        """Generate validation report"""
        # Load JSON data
        if not self.load_json():
            return {
                'file': self.json_file,
                'timestamp': datetime.now().isoformat(),
                'errors': self.errors,
                'warnings': self.warnings,
                'overall_valid': False
            }
        
        # Run validations
        schema_valid = self.validate_schema() if self.schema_file else None
        content_valid = self.validate_business_rules()
        types_valid = self.validate_data_types()
        
        # Calculate totals
        totals = self.calculate_totals()
        
        return {
            'file': self.json_file,
            'timestamp': datetime.now().isoformat(),
            'schema_validation': {
                'available': JSONSCHEMA_AVAILABLE,
                'schema_file': self.schema_file,
                'valid': schema_valid
            },
            'business_rules_valid': content_valid,
            'data_types_valid': types_valid,
            'calculated_totals': totals,
            'errors': self.errors,
            'warnings': self.warnings,
            'overall_valid': len(self.errors) == 0
        }
    
    def print_report(self):
        """Print validation report to console"""
        report = self.generate_report()
        
        print("=" * 60)
        print("JSON RECEIPT VALIDATION REPORT")
        print("=" * 60)
        print(f"File: {report['file']}")
        print(f"Timestamp: {report['timestamp']}")
        print(f"Overall Status: {'✓ VALID' if report['overall_valid'] else '✗ INVALID'}")
        print()
        
        # Schema validation
        if report['schema_validation']['available']:
            print(f"Schema Validation: {'✓ PASSED' if report['schema_validation']['valid'] else '✗ FAILED'}")
            print(f"  Schema File: {report['schema_validation']['schema_file']}")
        else:
            print(f"Schema Validation: ⚠ DISABLED (jsonschema not available)")
        
        print(f"Business Rules: {'✓ PASSED' if report['business_rules_valid'] else '✗ FAILED'}")
        print(f"Data Types: {'✓ PASSED' if report['data_types_valid'] else '✗ FAILED'}")
        print()
        
        # Calculated totals
        totals = report['calculated_totals']
        print("CALCULATED TOTALS:")
        print(f"  Items Count: {totals['item_count']}")
        print(f"  Calculated Subtotal: {totals['calculated_subtotal']:.2f}")
        print(f"  Taxable Subtotal: {totals['taxable_subtotal']:.2f}")
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
    
    parser = argparse.ArgumentParser(description='Validate JSON shopping receipt')
    parser.add_argument('json_file', help='Path to JSON receipt file')
    parser.add_argument('--schema', default='shopping_receipt_schema.json', 
                       help='Path to JSON schema file (default: shopping_receipt_schema.json)')
    parser.add_argument('--json-report', help='Save report to JSON file')
    parser.add_argument('--quiet', action='store_true', help='Suppress output')
    
    args = parser.parse_args()
    
    # Create validator
    validator = JSONReceiptValidator(args.json_file, args.schema)
    
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
        json_file = "shopping_receipt.json"
        schema_file = "shopping_receipt_schema.json"
        
        validator = JSONReceiptValidator(json_file, schema_file)
        validator.print_report()
    else:
        main()