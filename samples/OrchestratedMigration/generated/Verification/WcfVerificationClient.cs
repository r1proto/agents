using System;
using System.Collections.Generic;
using OrderService.Contracts;

namespace OrchestratedMigration.Verification
{
    /// <summary>
    /// Standalone verification client that calls the original WCF service
    /// to capture baseline results for comparison against the RabbitMQ implementation.
    /// </summary>
    internal static class WcfVerificationClient
    {
        private static void Main()
        {
            Console.WriteLine("[WCF] Starting WCF verification scenario...");

            OrderServiceClient proxy = null;
            string orderId = null;

            try
            {
                proxy = new OrderServiceClient("OrderServiceEndpoint");

                // Step 1 — PlaceOrder
                var request = new OrderRequest
                {
                    CustomerId = "VERIFY-001",
                    ShippingAddress = "1 Test St",
                    Lines = new List<OrderLine>
                    {
                        new OrderLine { ProductId = "PROD-X", Quantity = 2, UnitPrice = 15.00m },
                        new OrderLine { ProductId = "PROD-Y", Quantity = 1, UnitPrice = 30.00m }
                    }
                };

                var confirmation = proxy.PlaceOrder(request);
                orderId = confirmation.OrderId; // NOTE: OrderId differs per run
                Console.WriteLine($"[WCF] PlaceOrder | Success={confirmation.Success}, OrderId={confirmation.OrderId}, Total={confirmation.TotalAmount:C}");

                // Step 2 — GetOrderStatus
                var status = proxy.GetOrderStatus(orderId);
                Console.WriteLine($"[WCF] GetOrderStatus | OrderId={status.OrderId}, Status={status.Status}");

                // Step 3 — CancelOrder (one-way)
                proxy.CancelOrder(orderId);
                Console.WriteLine($"[WCF] CancelOrder | OrderId={orderId} | sent (one-way)");

                proxy.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WCF] ERROR: {ex.Message}");
                proxy?.Abort();
            }
        }
    }
}
