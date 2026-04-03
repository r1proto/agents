using System;
using System.Collections.Generic;

namespace OrchestratedMigration.Tests
{
    /// <summary>
    /// Pure business logic extracted from OrderServiceConsumer for unit testing.
    /// Has no RabbitMQ or I/O dependencies.
    /// </summary>
    public class OrderLogic
    {
        private readonly Dictionary<string, OrderRecord> _orders =
            new Dictionary<string, OrderRecord>(StringComparer.Ordinal);

        /// <summary>
        /// Places a new order, computes the total, and persists the order in memory.
        /// </summary>
        /// <param name="customerId">The customer identifier. Must not be null or whitespace.</param>
        /// <param name="lines">The order lines. Must contain at least one item.</param>
        /// <returns>An <see cref="OrderConfirmation"/> with the assigned order ID and total.</returns>
        /// <exception cref="ArgumentException">Thrown when customerId is null/empty or lines is null/empty.</exception>
        public OrderConfirmation PlaceOrder(string customerId, List<(string ProductId, int Qty, decimal Price)> lines)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("CustomerId is required.", nameof(customerId));

            if (lines == null || lines.Count == 0)
                throw new ArgumentException("At least one order line is required.", nameof(lines));

            var orderId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            decimal total = 0m;
            foreach (var line in lines)
                total += line.Qty * line.Price;

            _orders[orderId] = new OrderRecord
            {
                OrderId = orderId,
                Status = "Pending",
                TrackingNumber = null
            };

            return new OrderConfirmation
            {
                OrderId = orderId,
                TotalAmount = total,
                Success = true,
                Message = "Order placed successfully."
            };
        }

        /// <summary>
        /// Retrieves the current status record for an order.
        /// </summary>
        /// <param name="orderId">The order ID to look up.</param>
        /// <returns>The matching <see cref="OrderRecord"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the order ID is not found.</exception>
        public OrderRecord GetOrderStatus(string orderId)
        {
            if (!_orders.TryGetValue(orderId, out var record))
                throw new InvalidOperationException($"Order '{orderId}' not found.");

            return record;
        }

        /// <summary>
        /// Cancels an existing order by setting its status to "Cancelled".
        /// Silently ignores unknown order IDs.
        /// </summary>
        /// <param name="orderId">The order ID to cancel.</param>
        public void CancelOrder(string orderId)
        {
            if (_orders.TryGetValue(orderId, out var record))
                record.Status = "Cancelled";
        }
    }

    /// <summary>Immutable view returned by <see cref="OrderLogic.PlaceOrder"/>.</summary>
    public class OrderConfirmation
    {
        public string OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Internal order state persisted by <see cref="OrderLogic"/>.</summary>
    public class OrderRecord
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public string TrackingNumber { get; set; }
    }
}
