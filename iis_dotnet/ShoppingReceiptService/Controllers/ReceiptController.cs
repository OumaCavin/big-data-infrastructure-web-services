using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ShoppingReceiptService.Models;
using System.Text;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ShoppingReceiptService.Controllers
{
    /// <summary>
    /// API controller for managing shopping receipts
    /// Provides endpoints for retrieving, validating, and managing receipt data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json", "application/xml")]
    public class ReceiptController : ControllerBase
    {
        private readonly ILogger<ReceiptController> _logger;
        private readonly IWebHostEnvironment _environment;

        // In-memory storage for demo purposes (use database in production)
        private static readonly Dictionary<string, ShoppingReceipt> _receipts = new();

        public ReceiptController(ILogger<ReceiptController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            // Initialize with sample data
            InitializeSampleData();
        }

        /// <summary>
        /// Get all receipts (with optional filtering)
        /// </summary>
        /// <param name="storeName">Filter by store name</param>
        /// <param name="customerId">Filter by customer ID</param>
        /// <param name="dateFrom">Filter from date (YYYY-MM-DD)</param>
        /// <param name="dateTo">Filter to date (YYYY-MM-DD)</param>
        /// <returns>List of matching receipts</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ShoppingReceipt>), StatusCodes.Status200OK)]
        public IActionResult GetReceipts(
            [FromQuery] string? storeName = null,
            [FromQuery] string? customerId = null,
            [FromQuery] string? dateFrom = null,
            [FromQuery] string? dateTo = null)
        {
            try
            {
                var query = _receipts.Values.AsEnumerable();

                // Apply filters
                if (!string.IsNullOrEmpty(storeName))
                    query = query.Where(r => r.StoreName.Contains(storeName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(customerId))
                    query = query.Where(r => r.Customer.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                    query = query.Where(r => r.PurchaseDate.Date >= fromDate.Date);

                if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                    query = query.Where(r => r.PurchaseDate.Date <= toDate.Date);

                var result = query.OrderByDescending(r => r.PurchaseDate).ToList();

                _logger.LogInformation("Retrieved {Count} receipts with filters", result.Count);

                return Ok(ApiResponse<List<ShoppingReceipt>>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipts");
                return StatusCode(500, ApiResponse<ShoppingReceipt>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Get a specific receipt by ID
        /// </summary>
        /// <param name="id">Receipt ID</param>
        /// <returns>Receipt details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ShoppingReceipt), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetReceipt(string id)
        {
            try
            {
                if (_receipts.TryGetValue(id, out var receipt))
                {
                    _logger.LogInformation("Retrieved receipt {ReceiptId}", id);
                    return Ok(ApiResponse<ShoppingReceipt>.SuccessResult(receipt));
                }

                _logger.LogWarning("Receipt {ReceiptId} not found", id);
                return NotFound(ApiResponse<ShoppingReceipt>.ErrorResult($"Receipt with ID '{id}' not found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt {ReceiptId}", id);
                return StatusCode(500, ApiResponse<ShoppingReceipt>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Get receipt statistics
        /// </summary>
        /// <returns>Receipt statistics and metrics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetStatistics()
        {
            try
            {
                var receipts = _receipts.Values.ToList();
                
                var stats = new
                {
                    total_receipts = receipts.Count,
                    total_amount = receipts.Sum(r => r.TotalAmount),
                    average_amount = receipts.Count > 0 ? receipts.Average(r => r.TotalAmount) : 0,
                    store_distribution = receipts.GroupBy(r => r.StoreName)
                                                .ToDictionary(g => g.Key, g => g.Count()),
                    payment_methods = receipts.GroupBy(r => r.PaymentMethod)
                                             .ToDictionary(g => g.Key, g => g.Count()),
                    date_range = new
                    {
                        earliest = receipts.Count > 0 ? receipts.Min(r => r.PurchaseDate) : (DateTime?)null,
                        latest = receipts.Count > 0 ? receipts.Max(r => r.PurchaseDate) : (DateTime?)null
                    },
                    item_statistics = CalculateItemStatistics(receipts)
                };

                return Ok(ApiResponse<object>.SuccessResult(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating statistics");
                return StatusCode(500, ApiResponse<object>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Validate a receipt (XML or JSON format)
        /// </summary>
        /// <param name="request">Validation request containing receipt data</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult ValidateReceipt([FromBody] ValidationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ReceiptData))
                    return BadRequest(ApiResponse<ValidationResponse>.ErrorResult("Receipt data is required"));

                var response = new ValidationResponse
                {
                    IsValid = false,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };

                // Parse and validate based on format
                if (request.Format?.ToLower() == "json")
                {
                    response = ValidateJsonReceipt(request.ReceiptData);
                }
                else if (request.Format?.ToLower() == "xml")
                {
                    response = ValidateXmlReceipt(request.ReceiptData);
                }
                else
                {
                    response.Errors.Add("Invalid format. Use 'json' or 'xml'");
                    return BadRequest(ApiResponse<ValidationResponse>.ErrorResult(response.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating receipt");
                return StatusCode(500, ApiResponse<ValidationResponse>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Search receipts by items or description
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>Matching receipts</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<ShoppingReceipt>), StatusCodes.Status200OK)]
        public IActionResult SearchReceipts([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(ApiResponse<ShoppingReceipt>.ErrorResult("Search query is required"));

                var results = _receipts.Values
                    .Where(r => r.Items.Any(item => 
                        item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        item.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(r => r.PurchaseDate)
                    .ToList();

                _logger.LogInformation("Search for '{Query}' returned {Count} results", query, results.Count);

                return Ok(ApiResponse<List<ShoppingReceipt>>.SuccessResult(results));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching receipts");
                return StatusCode(500, ApiResponse<ShoppingReceipt>.ErrorResult("Internal server error"));
            }
        }

        /// <summary>
        /// Export receipt data in various formats
        /// </summary>
        /// <param name="id">Receipt ID</param>
        /// <param name="format">Export format (json, xml, csv)</param>
        /// <returns>Exported data</returns>
        [HttpGet("{id}/export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ExportReceipt(string id, [FromQuery] string format = "json")
        {
            try
            {
                if (!_receipts.TryGetValue(id, out var receipt))
                    return NotFound(ApiResponse<ShoppingReceipt>.ErrorResult($"Receipt with ID '{id}' not found"));

                format = format.ToLower();
                string content;
                string contentType;

                switch (format)
                {
                    case "json":
                        content = JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true });
                        contentType = "application/json";
                        break;

                    case "xml":
                        content = ConvertToXml(receipt);
                        contentType = "application/xml";
                        break;

                    case "csv":
                        content = ConvertToCsv(receipt);
                        contentType = "text/csv";
                        break;

                    default:
                        return BadRequest(ApiResponse<ShoppingReceipt>.ErrorResult("Invalid format. Use 'json', 'xml', or 'csv'"));
                }

                _logger.LogInformation("Exported receipt {ReceiptId} in {Format} format", id, format);

                return Content(content, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting receipt {ReceiptId}", id);
                return StatusCode(500, ApiResponse<ShoppingReceipt>.ErrorResult("Internal server error"));
            }
        }

        #region Private Methods

        private void InitializeSampleData()
        {
            if (_receipts.Count > 0) return; // Already initialized

            var sampleReceipt = new ShoppingReceipt
            {
                ReceiptId = "RCPT_2025_001",
                PurchaseDate = DateTime.UtcNow.AddDays(-1),
                StoreName = "SuperMarket Plus",
                CashierId = "CSH_001",
                PaymentMethod = "Credit Card",
                TotalAmount = 147.90m,
                Customer = new Customer
                {
                    CustomerId = "CUST_12345",
                    Name = "John Doe",
                    Phone = "+254712345678",
                    Email = "john.doe@email.com",
                    IsLoyaltyMember = true,
                    MemberSince = new DateTime(2020, 3, 15)
                },
                Store = new Store
                {
                    StoreId = "STORE_001",
                    Name = "SuperMarket Plus",
                    Address = "123 Main Street, Nairobi, Kenya",
                    City = "Nairobi",
                    Country = "Kenya",
                    PostalCode = "00100",
                    Phone = "+254201234567",
                    Email = "info@supermarketplus.co.ke",
                    TaxId = "TIN123456789"
                },
                Items = new List<ReceiptItem>
                {
                    new ReceiptItem
                    {
                        ItemId = "001",
                        Name = "Premium Coffee Beans",
                        Category = "Food & Beverages",
                        Description = "Organic Arabica Coffee Beans 1kg - Premium Grade",
                        Quantity = 2,
                        UnitPrice = 25.99m,
                        Subtotal = 51.98m,
                        IsTaxApplied = true,
                        IsDiscountEligible = true
                    },
                    new ReceiptItem
                    {
                        ItemId = "002",
                        Name = "Whole Wheat Bread",
                        Category = "Bakery",
                        Description = "Freshly baked whole wheat loaf - 800g",
                        Quantity = 1,
                        UnitPrice = 15.50m,
                        Subtotal = 15.50m,
                        IsTaxApplied = true,
                        IsDiscountEligible = false
                    }
                },
                Pricing = new Pricing
                {
                    Subtotal = 67.48m,
                    SubtotalExclTax = 58.68m,
                    GrandTotal = 147.90m
                },
                Payment = new Payment
                {
                    Method = "Credit Card",
                    CardType = "Visa",
                    CardNumberLastFour = "4567",
                    AuthorizationCode = "AUTH123456789",
                    TransactionId = "TXN789123456",
                    PaymentTime = DateTime.UtcNow.AddDays(-1),
                    ApprovedAmount = 147.90m
                },
                Loyalty = new Loyalty
                {
                    PointsEarned = 15,
                    PointsRedeemed = 0,
                    TotalPointsBalance = 342,
                    MemberTier = "Gold"
                },
                Transaction = new TransactionInfo
                {
                    TransactionId = "TXN_2025_001_456",
                    ReceiptNumber = "RCPT_2025_001_456",
                    TerminalId = "TERM_001",
                    CashierId = "CSH_001",
                    ShiftId = "SHIFT_001",
                    RegisterNumber = "REG_003"
                },
                Footer = new FooterInfo
                {
                    ReceiptTime = DateTime.UtcNow.AddDays(-1),
                    ThankYouMessage = "Thank you for shopping with us!",
                    ReturnPolicy = "30-day return policy with original receipt"
                },
                DigitalSignature = new DigitalSignature
                {
                    Algorithm = "SHA256",
                    Signature = "a1b2c3d4e5f6...",
                    Timestamp = DateTime.UtcNow.AddDays(-1)
                }
            };

            _receipts[sampleReceipt.ReceiptId] = sampleReceipt;
        }

        private ValidationResponse ValidateJsonReceipt(string jsonData)
        {
            var response = new ValidationResponse
            {
                IsValid = false,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            try
            {
                var document = JsonDocument.Parse(jsonData);
                var root = document.RootElement;

                // Check for required fields
                var requiredFields = new[] { "receipt_id", "purchase_date", "store_name", "items", "pricing", "payment" };
                foreach (var field in requiredFields)
                {
                    if (!root.TryGetProperty(field, out _))
                        response.Errors.Add($"Missing required field: {field}");
                }

                // Validate items array
                if (root.TryGetProperty("items", out var items) && items.GetArrayLength() < 5)
                    response.Errors.Add("Receipt must contain at least 5 items");

                // Calculate totals
                var totals = CalculateJsonTotals(root);
                response.CalculatedTotals = totals;

                response.IsValid = response.Errors.Count == 0;
            }
            catch (JsonException ex)
            {
                response.Errors.Add($"Invalid JSON format: {ex.Message}");
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Validation error: {ex.Message}");
            }

            return response;
        }

        private ValidationResponse ValidateXmlReceipt(string xmlData)
        {
            var response = new ValidationResponse
            {
                IsValid = false,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            try
            {
                var doc = XDocument.Parse(xmlData);
                var root = doc.Root;

                if (root == null)
                {
                    response.Errors.Add("Invalid XML: Root element not found");
                    return response;
                }

                // Check for required elements
                var requiredElements = new[] { "customer", "store", "items", "pricing", "payment" };
                foreach (var element in requiredElements)
                {
                    if (root.Element(element) == null)
                        response.Errors.Add($"Missing required element: {element}");
                }

                // Check items count
                var items = root.Element("items")?.Elements("item");
                if (items == null || items.Count() < 5)
                    response.Errors.Add("Receipt must contain at least 5 items");

                response.IsValid = response.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Invalid XML format: {ex.Message}");
            }

            return response;
        }

        private object CalculateJsonTotals(JsonElement root)
        {
            decimal subtotal = 0;
            int itemCount = 0;

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("subtotal", out var subtotalElement))
                    {
                        subtotal += subtotalElement.GetDecimal();
                        itemCount++;
                    }
                }
            }

            return new
            {
                calculated_subtotal = subtotal,
                item_count = itemCount
            };
        }

        private string ConvertToXml(ShoppingReceipt receipt)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<shopping_receipt>");
            
            // Add customer info
            xml.AppendLine("  <customer>");
            xml.AppendLine($"    <customer_id>{receipt.Customer.CustomerId}</customer_id>");
            xml.AppendLine($"    <name>{System.Security.SecurityElement.Escape(receipt.Customer.Name)}</name>");
            xml.AppendLine($"    <email>{System.Security.SecurityElement.Escape(receipt.Customer.Email)}</email>");
            xml.AppendLine("  </customer>");
            
            // Add items
            xml.AppendLine("  <items>");
            foreach (var item in receipt.Items)
            {
                xml.AppendLine("    <item>");
                xml.AppendLine($"      <name>{System.Security.SecurityElement.Escape(item.Name)}</name>");
                xml.AppendLine($"      <quantity>{item.Quantity}</quantity>");
                xml.AppendLine($"      <unit_price>{item.UnitPrice}</unit_price>");
                xml.AppendLine($"      <subtotal>{item.Subtotal}</subtotal>");
                xml.AppendLine("    </item>");
            }
            xml.AppendLine("  </items>");
            
            xml.AppendLine("</shopping_receipt>");
            
            return xml.ToString();
        }

        private string ConvertToCsv(ShoppingReceipt receipt)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Item Name,Quantity,Unit Price,Subtotal,Category");
            
            // Items
            foreach (var item in receipt.Items)
            {
                csv.AppendLine($"\"{item.Name}\",{item.Quantity},{item.UnitPrice},{item.Subtotal},\"{item.Category}\"");
            }
            
            // Total
            csv.AppendLine($"TOTAL,,,{receipt.TotalAmount},");
            
            return csv.ToString();
        }

        private object CalculateItemStatistics(List<ShoppingReceipt> receipts)
        {
            var allItems = receipts.SelectMany(r => r.Items).ToList();
            
            return new
            {
                total_items = allItems.Count,
                average_item_price = allItems.Count > 0 ? allItems.Average(i => i.UnitPrice) : 0,
                most_popular_categories = allItems
                    .GroupBy(i => i.Category)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        #endregion
    }
}