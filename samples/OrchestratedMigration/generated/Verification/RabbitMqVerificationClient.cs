using System;
using System.Collections.Generic;
using OrchestratedMigration.Client;
using OrchestratedMigration.Infrastructure;
using OrchestratedMigration.Messages;

namespace OrchestratedMigration.Verification
{
    /// <summary>
    /// Standalone verification client that sends the identical scenario as
    /// <see cref="WcfVerificationClient"/> to the RabbitMQ implementation
    /// so results can be compared side-by-side.
    /// </summary>
    internal static class RabbitMqVerificationClient
    {
        private static void Main()
        {
            Console.WriteLine("[RabbitMQ] Starting RabbitMQ verification scenario...");

            try
            {
                var factory = RabbitMqConnectionFactory.Create();
                using (var connection = factory.CreateConnection())
                using (var client = new OrderServiceRabbitMqClient(connection))
                {
                    // Step 1 — PlaceOrder (identical input to WcfVerificationClient)
                    var placeMsg = new PlaceOrderMessage
                    {
                        CustomerId = "VERIFY-001",
                        ShippingAddress = "1 Test St",
                        Lines = new List<OrderLineDto>
                        {
                            new OrderLineDto { ProductId = "PROD-X", Quantity = 2, UnitPrice = 15.00m },
                            new OrderLineDto { ProductId = "PROD-Y", Quantity = 1, UnitPrice = 30.00m }
                        }
                    };

                    var placeResp = client.PlaceOrderAsync(placeMsg).GetAwaiter().GetResult();
                    var orderId = placeResp.OrderId; // NOTE: OrderId differs per run
                    Console.WriteLine($"[RabbitMQ] PlaceOrder | Success={placeResp.Success}, OrderId={placeResp.OrderId}, Total={placeResp.TotalAmount:C}");

                    // Step 2 — GetOrderStatus
                    var statusMsg = new GetOrderStatusMessage { OrderId = orderId };
                    var statusResp = client.GetOrderStatusAsync(statusMsg).GetAwaiter().GetResult();
                    Console.WriteLine($"[RabbitMQ] GetOrderStatus | OrderId={statusResp.OrderId}, Status={statusResp.Status}");

                    // Step 3 — CancelOrder (fire-and-forget)
                    var cancelMsg = new CancelOrderMessage { OrderId = orderId };
                    client.CancelOrder(cancelMsg);
                    Console.WriteLine($"[RabbitMQ] CancelOrder | OrderId={orderId} | sent (one-way)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ] ERROR: {ex.Message}");
            }
        }
    }
}
