/// <summary>
/// Orchestration Pattern Implementation for Web Services
/// 
/// This class demonstrates the ORCHESTRATION pattern where a central service
/// (orchestrator) controls and coordinates multiple web services to complete
/// a business process (in this case, order processing).
/// 
/// Key characteristics of Orchestration:
/// - Centralized control flow
/// - Deterministic workflow
/// - One service coordinates others
/// - Top-down design approach
/// - Easier to monitor and debug
/// 
/// Author: Cavin Otieno
/// Course: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing
/// Date: 2025-11-27
/// </summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ChoreographyOrchestration.Orchestration
{
    /// <summary>
    /// Order Orchestrator Service - Centralized Control Pattern
    /// Coordinates multiple web services to process orders
    /// </summary>
    [ApiController]
    [Route("api/orchestration/[controller]")]
    public class OrderOrchestratorController : ControllerBase
    {
        private readonly ILogger<OrderOrchestratorController> _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _serviceEndpoints;

        public OrderOrchestratorController(ILogger<OrderOrchestratorController> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            
            // Service endpoint configuration (in production, this would be from config/registry)
            _serviceEndpoints = new Dictionary<string, string>
            {
                { "inventory", "http://localhost:5001/api/inventory" },
                { "payment", "http://localhost:5002/api/payment" },
                { "shipping", "http://localhost:5003/api/shipping" },
                { "notification", "http://localhost:5004/api/notification" },
                { "loyalty", "http://localhost:5005/api/loyalty" }
            };
        }

        /// <summary>
        /// Process order using orchestration pattern
        /// The orchestrator coordinates all services in a predetermined sequence
        /// </summary>
        /// <param name="orderRequest">Order details</param>
        /// <returns>Order processing result</returns>
        [HttpPost("process-order")]
        [ProducesResponseType(typeof(OrderProcessingResult), 200)]
        [ProducesResponseType(typeof(OrderProcessingResult), 400)]
        public async Task<IActionResult> ProcessOrder([FromBody] OrderRequest orderRequest)
        {
            var orchestrationLog = new List<string>();
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Starting order orchestration for OrderId: {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Order orchestration started");

                // Step 1: Validate Inventory (Sequential Execution)
                _logger.LogInformation("Step 1: Validating inventory for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 1: Validating inventory");
                
                var inventoryValidation = await ValidateInventory(orderRequest);
                if (!inventoryValidation.Success)
                {
                    orchestrationLog.Add("Step 1 FAILED: Inventory validation failed");
                    return BadRequest(CreateOrderResult(orderRequest.OrderId, false, orchestrationLog, 
                        $"Inventory validation failed: {inventoryValidation.Message}"));
                }
                orchestrationLog.Add("Step 1 COMPLETED: Inventory validated successfully");

                // Step 2: Calculate Pricing (Sequential Execution)
                _logger.LogInformation("Step 2: Calculating pricing for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 2: Calculating pricing");
                
                var pricingResult = await CalculatePricing(orderRequest);
                if (!pricingResult.Success)
                {
                    orchestrationLog.Add("Step 2 FAILED: Pricing calculation failed");
                    return BadRequest(CreateOrderResult(orderRequest.OrderId, false, orchestrationLog,
                        $"Pricing calculation failed: {pricingResult.Message}"));
                }
                orchestrationLog.Add("Step 2 COMPLETED: Pricing calculated successfully");

                // Step 3: Process Payment (Sequential Execution)
                _logger.LogInformation("Step 3: Processing payment for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 3: Processing payment");
                
                var paymentResult = await ProcessPayment(orderRequest, pricingResult.TotalAmount);
                if (!paymentResult.Success)
                {
                    orchestrationLog.Add("Step 3 FAILED: Payment processing failed");
                    return BadRequest(CreateOrderResult(orderRequest.OrderId, false, orchestrationLog,
                        $"Payment processing failed: {paymentResult.Message}"));
                }
                orchestrationLog.Add("Step 3 COMPLETED: Payment processed successfully");

                // Step 4: Reserve Inventory (Sequential Execution)
                _logger.LogInformation("Step 4: Reserving inventory for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 4: Reserving inventory");
                
                var inventoryReservation = await ReserveInventory(orderRequest);
                if (!inventoryReservation.Success)
                {
                    // ROLLBACK: Reverse payment if inventory reservation fails
                    await ReversePayment(paymentResult.TransactionId);
                    orchestrationLog.Add("Step 4 FAILED: Inventory reservation failed - payment reversed");
                    return BadRequest(CreateOrderResult(orderRequest.OrderId, false, orchestrationLog,
                        $"Inventory reservation failed: {inventoryReservation.Message} - Payment reversed"));
                }
                orchestrationLog.Add("Step 4 COMPLETED: Inventory reserved successfully");

                // Step 5: Update Loyalty Points (Sequential Execution)
                _logger.LogInformation("Step 5: Updating loyalty points for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 5: Updating loyalty points");
                
                var loyaltyUpdate = await UpdateLoyaltyPoints(orderRequest);
                if (!loyaltyUpdate.Success)
                {
                    _logger.LogWarning("Loyalty update failed but continuing: {Message}", loyaltyUpdate.Message);
                    orchestrationLog.Add("Step 5 WARNING: Loyalty update failed but continuing");
                }
                else
                {
                    orchestrationLog.Add("Step 5 COMPLETED: Loyalty points updated successfully");
                }

                // Step 6: Schedule Shipping (Sequential Execution)
                _logger.LogInformation("Step 6: Scheduling shipping for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 6: Scheduling shipping");
                
                var shippingSchedule = await ScheduleShipping(orderRequest);
                if (!shippingSchedule.Success)
                {
                    // ROLLBACK: Reverse inventory reservation
                    await ReleaseInventoryReservation(orderRequest.OrderId);
                    // ROLLBACK: Reverse payment
                    await ReversePayment(paymentResult.TransactionId);
                    orchestrationLog.Add("Step 6 FAILED: Shipping schedule failed - inventory released and payment reversed");
                    return BadRequest(CreateOrderResult(orderRequest.OrderId, false, orchestrationLog,
                        $"Shipping scheduling failed: {shippingSchedule.Message} - Inventory released and payment reversed"));
                }
                orchestrationLog.Add("Step 6 COMPLETED: Shipping scheduled successfully");

                // Step 7: Send Notifications (Sequential Execution)
                _logger.LogInformation("Step 7: Sending notifications for order {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add("Step 7: Sending notifications");
                
                var notifications = await SendOrderNotifications(orderRequest, shippingSchedule.TrackingNumber);
                orchestrationLog.AddRange(notifications);

                // Final result
                var duration = DateTime.UtcNow - startTime;
                orchestrationLog.Add($"Order orchestration completed successfully in {duration.TotalSeconds:F2} seconds");

                var result = CreateOrderResult(orderRequest.OrderId, true, orchestrationLog);
                result.TrackingNumber = shippingSchedule.TrackingNumber;
                result.PaymentTransactionId = paymentResult.TransactionId;
                result.EstimatedDelivery = shippingSchedule.EstimatedDelivery;

                _logger.LogInformation("Order orchestration completed successfully for OrderId: {OrderId}", orderRequest.OrderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order orchestration failed for OrderId: {OrderId}", orderRequest.OrderId);
                orchestrationLog.Add($"Orchestration failed with error: {ex.Message}");
                
                return StatusCode(500, CreateOrderResult(orderRequest.OrderId, false, orchestrationLog,
                    $"Orchestration error: {ex.Message}"));
            }
        }

        #region Orchestration Steps

        /// <summary>
        /// Step 1: Validate inventory availability
        /// </summary>
        private async Task<ServiceResponse> ValidateInventory(OrderRequest order)
        {
            var requestData = new
            {
                order.Items,
                order.CustomerId,
                priority = order.Priority
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["inventory"]}/validate", requestData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse>();
                    return result ?? new ServiceResponse { Success = false, Message = "Empty response from inventory service" };
                }
                else
                {
                    return new ServiceResponse { Success = false, Message = $"Inventory service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Inventory service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 2: Calculate order pricing with taxes and discounts
        /// </summary>
        private async Task<PricingResult> CalculatePricing(OrderRequest order)
        {
            var requestData = new
            {
                order.Items,
                order.CustomerId,
                order.PromoCode
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["payment"]}/calculate-pricing", requestData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PricingResult>();
                    return result ?? new PricingResult { Success = false, Message = "Empty response from pricing service" };
                }
                else
                {
                    return new PricingResult { Success = false, Message = $"Pricing service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new PricingResult { Success = false, Message = $"Pricing service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 3: Process payment
        /// </summary>
        private async Task<PaymentResult> ProcessPayment(OrderRequest order, decimal amount)
        {
            var paymentRequest = new
            {
                order.OrderId,
                order.CustomerId,
                Amount = amount,
                order.PaymentMethod,
                order.PaymentDetails
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["payment"]}/process", paymentRequest);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaymentResult>();
                    return result ?? new PaymentResult { Success = false, Message = "Empty response from payment service" };
                }
                else
                {
                    return new PaymentResult { Success = false, Message = $"Payment service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResult { Success = false, Message = $"Payment service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 4: Reserve inventory
        /// </summary>
        private async Task<ServiceResponse> ReserveInventory(OrderRequest order)
        {
            var requestData = new
            {
                order.OrderId,
                order.Items
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["inventory"]}/reserve", requestData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse>();
                    return result ?? new ServiceResponse { Success = false, Message = "Empty response from inventory reservation service" };
                }
                else
                {
                    return new ServiceResponse { Success = false, Message = $"Inventory reservation service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Inventory reservation service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 5: Update loyalty points
        /// </summary>
        private async Task<ServiceResponse> UpdateLoyaltyPoints(OrderRequest order)
        {
            var requestData = new
            {
                order.OrderId,
                order.CustomerId
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["loyalty"]}/update", requestData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse>();
                    return result ?? new ServiceResponse { Success = false, Message = "Empty response from loyalty service" };
                }
                else
                {
                    return new ServiceResponse { Success = false, Message = $"Loyalty service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse { Success = false, Message = $"Loyalty service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 6: Schedule shipping
        /// </summary>
        private async Task<ShippingResult> ScheduleShipping(OrderRequest order)
        {
            var requestData = new
            {
                order.OrderId,
                order.ShippingAddress,
                order.Items,
                order.Priority
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["shipping"]}/schedule", requestData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ShippingResult>();
                    return result ?? new ShippingResult { Success = false, Message = "Empty response from shipping service" };
                }
                else
                {
                    return new ShippingResult { Success = false, Message = $"Shipping service error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ShippingResult { Success = false, Message = $"Shipping service unreachable: {ex.Message}" };
            }
        }

        /// <summary>
        /// Step 7: Send order notifications
        /// </summary>
        private async Task<List<string>> SendOrderNotifications(OrderRequest order, string trackingNumber)
        {
            var notifications = new List<string>();
            
            try
            {
                // Send customer notification
                var customerNotification = new
                {
                    order.OrderId,
                    order.CustomerId,
                    order.Email,
                    Type = "order_confirmation",
                    trackingNumber
                };
                var customerResponse = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["notification"]}/send", customerNotification);
                notifications.Add($"Customer notification: {(customerResponse.IsSuccessStatusCode ? "SENT" : "FAILED")}");

                // Send admin notification
                var adminNotification = new
                {
                    order.OrderId,
                    Type = "new_order",
                    Priority = order.Priority
                };
                var adminResponse = await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["notification"]}/send", adminNotification);
                notifications.Add($"Admin notification: {(adminResponse.IsSuccessStatusCode ? "SENT" : "FAILED")}");
            }
            catch (Exception ex)
            {
                notifications.Add($"Notification error: {ex.Message}");
            }

            return notifications;
        }

        #endregion

        #region Compensation (Rollback) Operations

        /// <summary>
        /// Rollback operation: Release inventory reservation
        /// </summary>
        private async Task ReleaseInventoryReservation(string orderId)
        {
            try
            {
                await _httpClient.DeleteAsync($"{_serviceEndpoints["inventory"]}/release/{orderId}");
                _logger.LogInformation("Released inventory reservation for OrderId: {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release inventory reservation for OrderId: {OrderId}", orderId);
            }
        }

        /// <summary>
        /// Rollback operation: Reverse payment
        /// </summary>
        private async Task ReversePayment(string transactionId)
        {
            try
            {
                var reverseRequest = new { TransactionId = transactionId, Reason = "Order failed" };
                await _httpClient.PostAsJsonAsync($"{_serviceEndpoints["payment"]}/reverse", reverseRequest);
                _logger.LogInformation("Reversed payment for TransactionId: {TransactionId}", transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reverse payment for TransactionId: {TransactionId}", transactionId);
            }
        }

        #endregion

        #region Helper Methods

        private OrderProcessingResult CreateOrderResult(string orderId, bool success, List<string> orchestrationLog, string? errorMessage = null)
        {
            return new OrderProcessingResult
            {
                OrderId = orderId,
                Success = success,
                OrchestrationLog = orchestrationLog,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// Order request model
    /// </summary>
    public class OrderRequest
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public string PaymentMethod { get; set; } = string.Empty;
        public object? PaymentDetails { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public object? ShippingAddress { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Order item model
    /// </summary>
    public class OrderItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// Service response model
    /// </summary>
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Pricing calculation result
    /// </summary>
    public class PricingResult : ServiceResponse
    {
        public decimal TotalAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
    }

    /// <summary>
    /// Payment processing result
    /// </summary>
    public class PaymentResult : ServiceResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shipping scheduling result
    /// </summary>
    public class ShippingResult : ServiceResponse
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime EstimatedDelivery { get; set; }
        public string Carrier { get; set; } = string.Empty;
    }

    /// <summary>
    /// Order processing result
    /// </summary>
    public class OrderProcessingResult
    {
        public string OrderId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<string> OrchestrationLog { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TrackingNumber { get; set; }
        public string? PaymentTransactionId { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }

    #endregion
}