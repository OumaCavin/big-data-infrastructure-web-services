using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShoppingReceiptService.Models
{
    /// <summary>
    /// Main shopping receipt data model
    /// </summary>
    public class ShoppingReceipt
    {
        [Required]
        [JsonPropertyName("receipt_id")]
        public string ReceiptId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("purchase_date")]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [JsonPropertyName("store_name")]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("cashier_id")]
        public string CashierId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        public Customer Customer { get; set; } = new Customer();

        [Required]
        public Store Store { get; set; } = new Store();

        [Required]
        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();

        [Required]
        public Pricing Pricing { get; set; } = new Pricing();

        [Required]
        public Payment Payment { get; set; } = new Payment();

        [JsonPropertyName("loyalty")]
        public Loyalty? Loyalty { get; set; }

        [Required]
        public TransactionInfo Transaction { get; set; } = new TransactionInfo();

        [Required]
        public FooterInfo Footer { get; set; } = new FooterInfo();

        [Required]
        public DigitalSignature DigitalSignature { get; set; } = new DigitalSignature();
    }

    /// <summary>
    /// Customer information
    /// </summary>
    public class Customer
    {
        [Required]
        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("loyalty_member")]
        public bool IsLoyaltyMember { get; set; }

        [JsonPropertyName("member_since")]
        public DateTime? MemberSince { get; set; }
    }

    /// <summary>
    /// Store information
    /// </summary>
    public class Store
    {
        [Required]
        [JsonPropertyName("store_id")]
        public string StoreId { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("tax_id")]
        public string TaxId { get; set; } = string.Empty;

        [JsonPropertyName("business_license")]
        public string? BusinessLicense { get; set; }
    }

    /// <summary>
    /// Individual shopping item
    /// </summary>
    public class ReceiptItem
    {
        [Required]
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("subcategory")]
        public string? Subcategory { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Barcode { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("tax_applied")]
        public bool IsTaxApplied { get; set; }

        [JsonPropertyName("discount_eligible")]
        public bool IsDiscountEligible { get; set; }
    }

    /// <summary>
    /// Pricing and tax information
    /// </summary>
    public class Pricing
    {
        [Required]
        public decimal Subtotal { get; set; }

        [Required]
        [JsonPropertyName("subtotal_excl_tax")]
        public decimal SubtotalExclTax { get; set; }

        [JsonPropertyName("tax_details")]
        public TaxDetails? TaxDetails { get; set; }

        public Discounts? Discounts { get; set; }

        public Charges? Charges { get; set; }

        [Required]
        [JsonPropertyName("grand_total")]
        public decimal GrandTotal { get; set; }

        [JsonPropertyName("rounding_adjustment")]
        public decimal? RoundingAdjustment { get; set; }
    }

    /// <summary>
    /// Tax breakdown information
    /// </summary>
    public class TaxDetails
    {
        public List<TaxItem> Tax { get; set; } = new List<TaxItem>();
    }

    /// <summary>
    /// Individual tax item
    /// </summary>
    public class TaxItem
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(0, 1, ErrorMessage = "Tax rate must be between 0 and 1")]
        public decimal Rate { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Tax amount must be non-negative")]
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Discount information
    /// </summary>
    public class Discounts
    {
        public List<DiscountItem> Discount { get; set; } = new List<DiscountItem>();
    }

    /// <summary>
    /// Individual discount item
    /// </summary>
    public class DiscountItem
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }
    }

    /// <summary>
    /// Additional charges
    /// </summary>
    public class Charges
    {
        public List<ChargeItem> Charge { get; set; } = new List<ChargeItem>();
    }

    /// <summary>
    /// Individual charge item
    /// </summary>
    public class ChargeItem
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Charge amount must be non-negative")]
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Payment information
    /// </summary>
    public class Payment
    {
        [Required]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("card_type")]
        public string? CardType { get; set; }

        [JsonPropertyName("card_number_last_four")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Card number must be exactly 4 digits")]
        public string? CardNumberLastFour { get; set; }

        [JsonPropertyName("card_holder_name")]
        public string? CardHolderName { get; set; }

        [Required]
        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("payment_time")]
        public DateTime PaymentTime { get; set; }

        [Required]
        [JsonPropertyName("approved_amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Approved amount must be non-negative")]
        public decimal ApprovedAmount { get; set; }
    }

    /// <summary>
    /// Loyalty program information
    /// </summary>
    public class Loyalty
    {
        [JsonPropertyName("points_earned")]
        [Range(0, int.MaxValue, ErrorMessage = "Points earned must be non-negative")]
        public int PointsEarned { get; set; }

        [JsonPropertyName("points_redeemed")]
        [Range(0, int.MaxValue, ErrorMessage = "Points redeemed must be non-negative")]
        public int PointsRedeemed { get; set; }

        [JsonPropertyName("total_points_balance")]
        [Range(0, int.MaxValue, ErrorMessage = "Total points balance must be non-negative")]
        public int TotalPointsBalance { get; set; }

        [JsonPropertyName("member_tier")]
        public string? MemberTier { get; set; }
    }

    /// <summary>
    /// Transaction details
    /// </summary>
    public class TransactionInfo
    {
        [Required]
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("receipt_number")]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("terminal_id")]
        public string TerminalId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("cashier_id")]
        public string CashierId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("shift_id")]
        public string ShiftId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("register_number")]
        public string RegisterNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Footer information
    /// </summary>
    public class FooterInfo
    {
        [Required]
        [JsonPropertyName("receipt_time")]
        public DateTime ReceiptTime { get; set; }

        [JsonPropertyName("print_time")]
        public DateTime? PrintTime { get; set; }

        [Required]
        [JsonPropertyName("thank_you_message")]
        public string ThankYouMessage { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("return_policy")]
        public string ReturnPolicy { get; set; } = string.Empty;

        [JsonPropertyName("feedback_message")]
        public string? FeedbackMessage { get; set; }

        public string? Website { get; set; }

        [JsonPropertyName("social_media")]
        public SocialMedia? SocialMedia { get; set; }
    }

    /// <summary>
    /// Social media information
    /// </summary>
    public class SocialMedia
    {
        public string? Facebook { get; set; }
        public string? Twitter { get; set; }
        public string? Instagram { get; set; }
    }

    /// <summary>
    /// Digital signature for authenticity
    /// </summary>
    public class DigitalSignature
    {
        [Required]
        public string Algorithm { get; set; } = string.Empty;

        [Required]
        public string Signature { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// API response model
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> ErrorResult(string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        public static ApiResponse<T> ErrorResult(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Request model for receipt validation
    /// </summary>
    public class ValidationRequest
    {
        [Required]
        public string ReceiptData { get; set; } = string.Empty;

        public string Format { get; set; } = "json"; // json or xml
    }

    /// <summary>
    /// Response model for receipt validation
    /// </summary>
    public class ValidationResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public object? CalculatedTotals { get; set; }
    }
}