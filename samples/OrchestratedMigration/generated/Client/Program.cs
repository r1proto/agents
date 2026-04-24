using System;
using System.Collections.Generic;
using OrchestratedMigration.Infrastructure;
using OrchestratedMigration.Messages;

namespace OrchestratedMigration.Client
{
    /// <summary>Demo client that exercises all three order operations against RabbitMQ.</summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("[Client] Connecting to RabbitMQ...");

            var factory = RabbitMqConnectionFactory.Create();
            using (var connection = factory.CreateConnection())
            using (var client = new OrderServiceRabbitMqClient(connection))
            {
                // 1. PlaceOrder
                var placeMsg = new PlaceOrderMessage
                {
                    CustomerId = "DEMO-001",
                    ShippingAddress = "1 Demo Lane",
                    Lines = new List<OrderLineDto>
                    {
                        new OrderLineDto { ProductId = "PROD-A", Quantity = 2, UnitPrice = 10.00m },
                        new OrderLineDto { ProductId = "PROD-B", Quantity = 1, UnitPrice = 5.50m }
                    }
                };

                Console.WriteLine("[Client] Sending PlaceOrder...");
                var placeResp = client.PlaceOrderAsync(placeMsg).GetAwaiter().GetResult();
                Console.WriteLine($"[Client] PlaceOrder | Success={placeResp.Success}, OrderId={placeResp.OrderId}, Total={placeResp.TotalAmount:C}");

                if (!placeResp.Success || string.IsNullOrEmpty(placeResp.OrderId))
                {
                    Console.Error.WriteLine("[Client] PlaceOrder failed. Aborting.");
                    return;
                }

                // 2. GetOrderStatus
                var statusMsg = new GetOrderStatusMessage { OrderId = placeResp.OrderId };
                Console.WriteLine("[Client] Sending GetOrderStatus...");
                var statusResp = client.GetOrderStatusAsync(statusMsg).GetAwaiter().GetResult();
                Console.WriteLine($"[Client] GetOrderStatus | OrderId={statusResp.OrderId}, Status={statusResp.Status}");

                // 3. CancelOrder (fire-and-forget)
                var cancelMsg = new CancelOrderMessage { OrderId = placeResp.OrderId };
                client.CancelOrder(cancelMsg);
                Console.WriteLine($"[Client] CancelOrder | OrderId={cancelMsg.OrderId} | sent (one-way)");
            }

            Console.WriteLine("[Client] Done.");
        }
    }
}
