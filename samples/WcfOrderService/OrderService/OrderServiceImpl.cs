using System;
using System.Collections.Generic;
using System.ServiceModel;
using OrderService.Contracts;

namespace OrderService
{
    /// <summary>
    /// WCF service implementation for order management.
    /// Hosted by ServiceHost in Program.cs.
    /// </summary>
    public class OrderServiceImpl : IOrderService
    {
        // In-memory store — simulates a real database.
        private static readonly Dictionary<string, OrderStatus> _orders =
            new Dictionary<string, OrderStatus>();

        public OrderConfirmation PlaceOrder(OrderRequest request)
        {
            if (request == null)
                throw new FaultException("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Field = "CustomerId", Reason = "CustomerId is required." });

            if (request.Lines == null || request.Lines.Count == 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Field = "Lines", Reason = "At least one order line is required." });

            var orderId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            decimal total = 0;
            foreach (var line in request.Lines)
                total += line.Quantity * line.UnitPrice;

            _orders[orderId] = new OrderStatus
            {
                OrderId = orderId,
                Status = "Pending",
                TrackingNumber = null
            };

            Console.WriteLine($"[OrderService] PlaceOrder: orderId={orderId}, customer={request.CustomerId}, total={total:C}");

            return new OrderConfirmation
            {
                OrderId = orderId,
                Success = true,
                Message = "Order placed successfully.",
                TotalAmount = total
            };
        }

        public OrderStatus GetOrderStatus(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new FaultException("orderId cannot be null or empty.");

            if (!_orders.TryGetValue(orderId, out var status))
                throw new FaultException($"Order '{orderId}' not found.");

            Console.WriteLine($"[OrderService] GetOrderStatus: orderId={orderId}, status={status.Status}");
            return status;
        }

        public void CancelOrder(string orderId)
        {
            // One-way: no reply. Fire-and-forget from the client's perspective.
            if (string.IsNullOrWhiteSpace(orderId))
            {
                Console.WriteLine("[OrderService] CancelOrder called with null/empty orderId — ignoring.");
                return;
            }

            if (_orders.TryGetValue(orderId, out var status))
            {
                status.Status = "Cancelled";
                Console.WriteLine($"[OrderService] CancelOrder: orderId={orderId} cancelled.");
            }
            else
            {
                Console.WriteLine($"[OrderService] CancelOrder: orderId={orderId} not found — ignoring.");
            }
        }
    }
}
