using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMqOrderService.Infrastructure;
using RabbitMqOrderService.Messages;

namespace RabbitMqOrderService.Client
{
    /// <summary>
    /// Demo client that exercises all three order operations against a live RabbitMQ broker.
    /// Mirrors the demonstration in the original WCF <c>OrderService.Client/Program.cs</c>.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            Console.WriteLine("RabbitMQ Order Service — demo client");
            Console.WriteLine($"Broker: {AppConfig.Host}:{AppConfig.Port}");
            Console.WriteLine();

            var factory = RabbitMqConnectionFactory.Create();

            using (var client = new OrderServiceRabbitMqClient(factory))
            {
                // ── 1. Place an order ────────────────────────────────────────────────
                Console.WriteLine("=== PlaceOrder ===");
                var placeMsg = new PlaceOrderMessage
                {
                    CustomerId      = "CUST-001",
                    ShippingAddress = "123 Main St, Springfield",
                    Lines = new List<OrderLineDto>
                    {
                        new OrderLineDto { ProductId = "PROD-A", Quantity = 2, UnitPrice = 10.00m },
                        new OrderLineDto { ProductId = "PROD-B", Quantity = 1, UnitPrice = 25.00m }
                    }
                };

                PlaceOrderResponse placeResp = await client.PlaceOrderAsync(placeMsg);
                Console.WriteLine($"Success:     {placeResp.Success}");
                Console.WriteLine($"OrderId:     {placeResp.OrderId}");
                Console.WriteLine($"TotalAmount: {placeResp.TotalAmount:C}");
                Console.WriteLine($"Message:     {placeResp.Message}");
                Console.WriteLine();

                if (!placeResp.Success || string.IsNullOrEmpty(placeResp.OrderId))
                {
                    Console.Error.WriteLine("PlaceOrder failed — aborting demo.");
                    return;
                }

                // ── 2. Get order status ──────────────────────────────────────────────
                Console.WriteLine("=== GetOrderStatus ===");
                var statusMsg = new GetOrderStatusMessage { OrderId = placeResp.OrderId };
                var statusResp = await client.GetOrderStatusAsync(statusMsg);
                Console.WriteLine($"OrderId:        {statusResp.OrderId}");
                Console.WriteLine($"Status:         {statusResp.Status}");
                Console.WriteLine($"TrackingNumber: {statusResp.TrackingNumber ?? "(none)"}");
                Console.WriteLine();

                // ── 3. Cancel the order ──────────────────────────────────────────────
                Console.WriteLine("=== CancelOrder (fire-and-forget) ===");
                client.CancelOrder(new CancelOrderMessage { OrderId = placeResp.OrderId });
                Console.WriteLine($"Cancel message sent for order {placeResp.OrderId}.");
                Console.WriteLine();

                Console.WriteLine("Demo complete. Press ENTER to exit.");
                Console.ReadLine();
            }
        }
    }
}
