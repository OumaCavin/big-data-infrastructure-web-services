/// <summary>
/// Choreography Pattern Implementation for Web Services
/// 
/// This class demonstrates the CHOREOGRAPHY pattern where services interact
/// through events without central coordination. Each service subscribes to
/// events and reacts independently, creating a distributed workflow.
/// 
/// Key characteristics of Choreography:
/// - Decentralized control flow
/// - Event-driven architecture
/// - Services react to events
/// - Bottom-up design approach
/// - Better scalability and flexibility
/// - More complex to monitor and debug
/// 
/// Author: Cavin Otieno
/// Course: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing
/// Date: 2025-11-27
/// </summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChoreographyOrchestration.Choreography
{
    /// <summary>
    /// Event Bus for Publishing and Subscribing to Events
    /// Simple in-memory implementation (use message broker like RabbitMQ in production)
    /// </summary>
    public interface IEventBus
    {
        Task PublishAsync<T>(T eventData) where T : class;
        void Subscribe<T>(Func<T, Task> handler) where T : class;
        void Unsubscribe<T>(Func<T, Task> handler) where T : class;
    }

    /// <summary>
    /// Simple in-memory event bus implementation
    /// </summary>
    public class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
        private readonly ILogger<InMemoryEventBus> _logger;

        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            
            if (_handlers.ContainsKey(eventType))
            {
                foreach (var handler in _handlers[eventType])
                {
                    try
                    {
                        await handler(eventData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling event {EventType}", eventType.Name);
                    }
                }
            }
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : class
        {
            var eventType = typeof(T);
            
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Func<object, Task>>();
            }
            
            _handlers[eventType].Add(evt => handler((T)evt));
        }

        public void Unsubscribe<T>(Func<T, Task> handler) where T : class
        {
            var eventType = typeof(T);
            
            if (_handlers.ContainsKey(eventType))
            {
                // Implementation for unsubscription would be more complex in real scenarios
            }
        }
    }

    /// <summary>
    /// Order Service - Publishes order events
    /// </summary>
    [ApiController]
    [Route("api/choreography/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IEventBus eventBus, ILogger<OrderController> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        /// <summary>
        /// Create order and publish OrderCreatedEvent
        /// This is the starting point of the choreography
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            try
            {
                _logger.LogInformation("Creating order {OrderId}", orderRequest.OrderId);

                // Save order to database (simulated)
                var order = await SaveOrderAsync(orderRequest);

                // Publish event - this triggers the choreography
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    Items = order.Items,
                    TotalAmount = order.TotalAmount,
                    ShippingAddress = order.ShippingAddress,
                    Priority = order.Priority,
                    Email = order.Email,
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(orderCreatedEvent);

                _logger.LogInformation("Published OrderCreatedEvent for order {OrderId}", order.OrderId);

                return Ok(new
                {
                    OrderId = order.OrderId,
                    Status = "Order created and event published",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order {OrderId}", orderRequest.OrderId);
                return BadRequest(new { Error = ex.Message });
            }
        }

        private async Task<ShoppingOrder> SaveOrderAsync(OrderRequest request)
        {
            // Simulate database save
            await Task.Delay(100);
            
            return new ShoppingOrder
            {
                OrderId = request.OrderId,
                CustomerId = request.CustomerId,
                Items = request.Items,
                TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice),
                ShippingAddress = request.ShippingAddress,
                Priority = request.Priority,
                Email = request.Email,
                Status = "Pending"
            };
        }
    }

    /// <summary>
    /// Inventory Service - Subscribes to OrderCreatedEvent
    /// </summary>
    public class InventoryService : IEventSubscriber
    {
        private readonly IEventBus _eventBus;
        private readonly HttpClient _httpClient;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IEventBus eventBus, ILogger<InventoryService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            _httpClient = new HttpClient();
            
            // Subscribe to events
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            // Subscribe to OrderCreatedEvent
            _eventBus.Subscribe<OrderCreatedEvent>(async orderEvent =>
            {
                await HandleOrderCreatedEvent(orderEvent);
            });

            // Subscribe to other relevant events
            _eventBus.Subscribe<PaymentProcessedEvent>(async paymentEvent =>
            {
                await HandlePaymentProcessedEvent(paymentEvent);
            });
        }

        private async Task HandleOrderCreatedEvent(OrderCreatedEvent orderEvent)
        {
            try
            {
                _logger.LogInformation("Inventory service processing OrderCreatedEvent for {OrderId}", orderEvent.OrderId);

                // Check inventory availability for each item
                var inventoryChecks = new List<Task<bool>>();
                
                foreach (var item in orderEvent.Items)
                {
                    var check = CheckInventoryAvailability(item.ItemId, item.Quantity);
                    inventoryChecks.Add(check);
                }

                // Wait for all inventory checks
                var results = await Task.WhenAll(inventoryChecks);
                var allAvailable = Array.TrueForAll(results, available => available);

                // Publish inventory check result
                var inventoryCheckedEvent = new InventoryCheckedEvent
                {
                    OrderId = orderEvent.OrderId,
                    AllItemsAvailable = allAvailable,
                    AvailableItems = orderEvent.Items.Where((item, index) => results[index]).ToList(),
                    UnavailableItems = orderEvent.Items.Where((item, index) => !results[index]).ToList(),
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(inventoryCheckedEvent);

                _logger.LogInformation("Published InventoryCheckedEvent for order {OrderId} - Available: {Available}", 
                    orderEvent.OrderId, allAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreatedEvent for {OrderId}", orderEvent.OrderId);
                
                // Publish error event
                var errorEvent = new OrderFailedEvent
                {
                    OrderId = orderEvent.OrderId,
                    FailureReason = $"Inventory service error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(errorEvent);
            }
        }

        private async Task HandlePaymentProcessedEvent(PaymentProcessedEvent paymentEvent)
        {
            if (!paymentEvent.Success) return;

            try
            {
                _logger.LogInformation("Inventory service processing PaymentProcessedEvent for {OrderId}", paymentEvent.OrderId);

                // Reserve inventory after successful payment
                var reservationResult = await ReserveInventory(paymentEvent.OrderId);

                // Publish inventory reserved event
                var inventoryReservedEvent = new InventoryReservedEvent
                {
                    OrderId = paymentEvent.OrderId,
                    ReservationId = reservationResult.ReservationId,
                    ReservedUntil = reservationResult.ReservedUntil,
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(inventoryReservedEvent);

                _logger.LogInformation("Published InventoryReservedEvent for order {OrderId}", paymentEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PaymentProcessedEvent for {OrderId}", paymentEvent.OrderId);
            }
        }

        private async Task<bool> CheckInventoryAvailability(string itemId, int quantity)
        {
            // Simulate inventory check
            await Task.Delay(50);
            
            // Simulate 95% availability rate
            var random = new Random();
            return random.Next(100) < 95;
        }

        private async Task<ReservationResult> ReserveInventory(string orderId)
        {
            // Simulate inventory reservation
            await Task.Delay(100);
            
            return new ReservationResult
            {
                ReservationId = $"RES_{orderId}",
                ReservedUntil = DateTime.UtcNow.AddHours(24)
            };
        }
    }

    /// <summary>
    /// Payment Service - Subscribes to InventoryCheckedEvent
    /// </summary>
    public class PaymentService : IEventSubscriber
    {
        private readonly IEventBus _eventBus;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IEventBus eventBus, ILogger<PaymentService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            _httpClient = new HttpClient();
            
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            // Subscribe to InventoryCheckedEvent
            _eventBus.Subscribe<InventoryCheckedEvent>(async inventoryEvent =>
            {
                await HandleInventoryCheckedEvent(inventoryEvent);
            });
        }

        private async Task HandleInventoryCheckedEvent(InventoryCheckedEvent inventoryEvent)
        {
            if (!inventoryEvent.AllItemsAvailable)
            {
                // Notify failure - not all items are available
                var failureEvent = new OrderFailedEvent
                {
                    OrderId = inventoryEvent.OrderId,
                    FailureReason = "Some items are not available in inventory",
                    Timestamp = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(failureEvent);
                return;
            }

            try
            {
                _logger.LogInformation("Payment service processing InventoryCheckedEvent for {OrderId}", inventoryEvent.OrderId);

                // Get order details to process payment
                var orderDetails = await GetOrderDetails(inventoryEvent.OrderId);

                // Process payment
                var paymentResult = await ProcessPayment(orderDetails);

                // Publish payment result event
                var paymentProcessedEvent = new PaymentProcessedEvent
                {
                    OrderId = inventoryEvent.OrderId,
                    Success = paymentResult.Success,
                    TransactionId = paymentResult.TransactionId,
                    Amount = paymentResult.Amount,
                    FailureReason = paymentResult.FailureReason,
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(paymentProcessedEvent);

                _logger.LogInformation("Published PaymentProcessedEvent for order {OrderId} - Success: {Success}", 
                    inventoryEvent.OrderId, paymentResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing InventoryCheckedEvent for {OrderId}", inventoryEvent.OrderId);

                var failureEvent = new OrderFailedEvent
                {
                    OrderId = inventoryEvent.OrderId,
                    FailureReason = $"Payment service error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(failureEvent);
            }
        }

        private async Task<ShoppingOrder> GetOrderDetails(string orderId)
        {
            // Simulate fetching order details from database
            await Task.Delay(50);
            
            return new ShoppingOrder
            {
                OrderId = orderId,
                TotalAmount = 147.90m, // Example amount
                CustomerId = "CUST_12345"
            };
        }

        private async Task<PaymentResult> ProcessPayment(ShoppingOrder order)
        {
            // Simulate payment processing
            await Task.Delay(200);
            
            // Simulate 98% success rate
            var random = new Random();
            var success = random.Next(100) < 98;
            
            return new PaymentResult
            {
                Success = success,
                TransactionId = success ? $"TXN_{Guid.NewGuid():N}" : null,
                Amount = order.TotalAmount,
                FailureReason = success ? null : "Payment declined"
            };
        }
    }

    /// <summary>
    /// Shipping Service - Subscribes to InventoryReservedEvent
    /// </summary>
    public class ShippingService : IEventSubscriber
    {
        private readonly IEventBus _eventBus;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(IEventBus eventBus, ILogger<ShippingService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            _httpClient = new HttpClient();
            
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            // Subscribe to InventoryReservedEvent
            _eventBus.Subscribe<InventoryReservedEvent>(async reservationEvent =>
            {
                await HandleInventoryReservedEvent(reservationEvent);
            });
        }

        private async Task HandleInventoryReservedEvent(InventoryReservedEvent reservationEvent)
        {
            try
            {
                _logger.LogInformation("Shipping service processing InventoryReservedEvent for {OrderId}", reservationEvent.OrderId);

                // Get order shipping details
                var shippingDetails = await GetShippingDetails(reservationEvent.OrderId);

                // Schedule shipping
                var shippingSchedule = await ScheduleShipping(shippingDetails);

                // Publish shipping scheduled event
                var shippingScheduledEvent = new ShippingScheduledEvent
                {
                    OrderId = reservationEvent.OrderId,
                    TrackingNumber = shippingSchedule.TrackingNumber,
                    EstimatedDelivery = shippingSchedule.EstimatedDelivery,
                    Carrier = shippingSchedule.Carrier,
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(shippingScheduledEvent);

                _logger.LogInformation("Published ShippingScheduledEvent for order {OrderId}", reservationEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing InventoryReservedEvent for {OrderId}", reservationEvent.OrderId);

                var failureEvent = new OrderFailedEvent
                {
                    OrderId = reservationEvent.OrderId,
                    FailureReason = $"Shipping service error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
                await _eventBus.PublishAsync(failureEvent);
            }
        }

        private async Task<ShippingDetails> GetShippingDetails(string orderId)
        {
            // Simulate fetching shipping details
            await Task.Delay(50);
            
            return new ShippingDetails
            {
                OrderId = orderId,
                Address = "123 Main St, Nairobi, Kenya",
                Priority = "Normal"
            };
        }

        private async Task<ShippingSchedule> ScheduleShipping(ShippingDetails details)
        {
            // Simulate shipping schedule calculation
            await Task.Delay(150);
            
            return new ShippingSchedule
            {
                TrackingNumber = $"TRK_{Guid.NewGuid():N}",
                EstimatedDelivery = DateTime.UtcNow.AddDays(3),
                Carrier = "Kenya Post"
            };
        }
    }

    /// <summary>
    /// Notification Service - Subscribes to final events
    /// </summary>
    public class NotificationService : IEventSubscriber
    {
        private readonly IEventBus _eventBus;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IEventBus eventBus, ILogger<NotificationService> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            _httpClient = new HttpClient();
            
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            // Subscribe to various completion events
            _eventBus.Subscribe<OrderCompletedEvent>(async completionEvent =>
            {
                await HandleOrderCompletedEvent(completionEvent);
            });

            _eventBus.Subscribe<OrderFailedEvent>(async failureEvent =>
            {
                await HandleOrderFailedEvent(failureEvent);
            });
        }

        private async Task HandleOrderCompletedEvent(OrderCompletedEvent completionEvent)
        {
            try
            {
                _logger.LogInformation("Notification service processing OrderCompletedEvent for {OrderId}", completionEvent.OrderId);

                // Send customer notification
                await SendCustomerNotification(completionEvent);

                // Send admin notification
                await SendAdminNotification(completionEvent);

                _logger.LogInformation("Sent notifications for completed order {OrderId}", completionEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for order {OrderId}", completionEvent.OrderId);
            }
        }

        private async Task HandleOrderFailedEvent(OrderFailedEvent failureEvent)
        {
            try
            {
                _logger.LogInformation("Notification service processing OrderFailedEvent for {OrderId}", failureEvent.OrderId);

                // Send failure notification to customer
                await SendFailureNotification(failureEvent);

                // Send admin alert
                await SendAdminAlert(failureEvent);

                _logger.LogInformation("Sent failure notifications for order {OrderId}", failureEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending failure notifications for order {OrderId}", failureEvent.OrderId);
            }
        }

        private async Task SendCustomerNotification(OrderCompletedEvent completionEvent)
        {
            // Simulate sending email/SMS
            await Task.Delay(100);
            _logger.LogInformation("Customer notification sent for order {OrderId}", completionEvent.OrderId);
        }

        private async Task SendAdminNotification(OrderCompletedEvent completionEvent)
        {
            // Simulate sending admin notification
            await Task.Delay(50);
            _logger.LogInformation("Admin notification sent for order {OrderId}", completionEvent.OrderId);
        }

        private async Task SendFailureNotification(OrderFailedEvent failureEvent)
        {
            // Simulate sending failure notification
            await Task.Delay(100);
            _logger.LogInformation("Failure notification sent for order {OrderId}: {Reason}", 
                failureEvent.OrderId, failureEvent.FailureReason);
        }

        private async Task SendAdminAlert(OrderFailedEvent failureEvent)
        {
            // Simulate sending admin alert
            await Task.Delay(50);
            _logger.LogInformation("Admin alert sent for failed order {OrderId}: {Reason}", 
                failureEvent.OrderId, failureEvent.FailureReason);
        }
    }

    /// <summary>
    /// Order Completion Coordinator - Subscribes to final events and publishes completion
    /// </summary>
    public class OrderCompletionCoordinator : IEventSubscriber
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<OrderCompletionCoordinator> _logger;
        private readonly Dictionary<string, List<string>> _completedSteps = new();

        public OrderCompletionCoordinator(IEventBus eventBus, ILogger<OrderCompletionCoordinator> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
            
            SubscribeToEvents();
        }

        public void SubscribeToEvents()
        {
            _eventBus.Subscribe<PaymentProcessedEvent>(async paymentEvent =>
            {
                await TrackStepCompletion(paymentEvent.OrderId, "Payment");
            });

            _eventBus.Subscribe<InventoryReservedEvent>(async inventoryEvent =>
            {
                await TrackStepCompletion(inventoryEvent.OrderId, "Inventory");
            });

            _eventBus.Subscribe<ShippingScheduledEvent>(async shippingEvent =>
            {
                await TrackStepCompletion(shippingEvent.OrderId, "Shipping");
            });

            _eventBus.Subscribe<OrderFailedEvent>(async failureEvent =>
            {
                await HandleOrderFailure(failureEvent);
            });
        }

        private async Task TrackStepCompletion(string orderId, string step)
        {
            if (!_completedSteps.ContainsKey(orderId))
            {
                _completedSteps[orderId] = new List<string>();
            }

            _completedSteps[orderId].Add(step);

            _logger.LogInformation("Order {OrderId} step '{Step}' completed. Steps: {Steps}", 
                orderId, step, string.Join(", ", _completedSteps[orderId]));

            // Check if all required steps are completed
            var requiredSteps = new[] { "Payment", "Inventory", "Shipping" };
            var completedSteps = _completedSteps[orderId];
            
            if (requiredSteps.All(step => completedSteps.Contains(step)))
            {
                // All steps completed - publish completion event
                var completionEvent = new OrderCompletedEvent
                {
                    OrderId = orderId,
                    CompletedSteps = completedSteps.ToList(),
                    Timestamp = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(completionEvent);
                _logger.LogInformation("Order {OrderId} fully completed and event published", orderId);
            }
        }

        private async Task HandleOrderFailure(OrderFailedEvent failureEvent)
        {
            // Clean up tracking data
            if (_completedSteps.ContainsKey(failureEvent.OrderId))
            {
                _completedSteps.Remove(failureEvent.OrderId);
            }

            _logger.LogInformation("Order {OrderId} failed: {Reason}", 
                failureEvent.OrderId, failureEvent.FailureReason);
        }
    }

    #region Event Models

    /// <summary>
    /// Base event interface
    /// </summary>
    public interface IEvent
    {
        DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Order created event
    /// </summary>
    public class OrderCreatedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public object? ShippingAddress { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Email { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Inventory checked event
    /// </summary>
    public class InventoryCheckedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public bool AllItemsAvailable { get; set; }
        public List<OrderItem> AvailableItems { get; set; } = new List<OrderItem>();
        public List<OrderItem> UnavailableItems { get; set; } = new List<OrderItem>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Payment processed event
    /// </summary>
    public class PaymentProcessedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? FailureReason { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Inventory reserved event
    /// </summary>
    public class InventoryReservedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string ReservationId { get; set; } = string.Empty;
        public DateTime ReservedUntil { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Shipping scheduled event
    /// </summary>
    public class ShippingScheduledEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime EstimatedDelivery { get; set; }
        public string Carrier { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Order completed event
    /// </summary>
    public class OrderCompletedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public List<string> CompletedSteps { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Order failed event
    /// </summary>
    public class OrderFailedEvent : IEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Supporting Models

    /// <summary>
    /// Event subscriber interface
    /// </summary>
    public interface IEventSubscriber
    {
        void SubscribeToEvents();
    }

    /// <summary>
    /// Shopping order model
    /// </summary>
    public class ShoppingOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal TotalAmount { get; set; }
        public object? ShippingAddress { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
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
    /// Payment result model
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Reservation result model
    /// </summary>
    public class ReservationResult
    {
        public string ReservationId { get; set; } = string.Empty;
        public DateTime ReservedUntil { get; set; }
    }

    /// <summary>
    /// Shipping details model
    /// </summary>
    public class ShippingDetails
    {
        public string OrderId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
    }

    /// <summary>
    /// Shipping schedule model
    /// </summary>
    public class ShippingSchedule
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime EstimatedDelivery { get; set; }
        public string Carrier { get; set; } = string.Empty;
    }

    /// <summary>
    /// Order request model
    /// </summary>
    public class OrderRequest
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public object? ShippingAddress { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}