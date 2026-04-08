using System;
using System.Collections.Generic;
using OrderService.Contracts;

namespace OrderService.Client
{
    /// <summary>
    /// Console app demonstrating use of the WCF client proxy.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = new OrderServiceClient("OrderServiceEndpoint"))
            {
                // 1. Place an order
                var request = new OrderRequest
                {
                    CustomerId = "CUST-001",
                    ShippingAddress = "123 Main St, Springfield",
                    Lines = new List<OrderLine>
                    {
                        new OrderLine { ProductId = "PROD-A", Quantity = 2, UnitPrice = 19.99m },
                        new OrderLine { ProductId = "PROD-B", Quantity = 1, UnitPrice = 49.99m }
                    }
                };

                OrderConfirmation confirmation = client.PlaceOrder(request);
                Console.WriteLine($"Order placed: {confirmation.OrderId}, Total: {confirmation.TotalAmount:C}");

                // 2. Check order status
                OrderStatus status = client.GetOrderStatus(confirmation.OrderId);
                Console.WriteLine($"Order status: {status.Status}");

                // 3. Cancel the order (one-way — no reply)
                client.CancelOrder(confirmation.OrderId);
                Console.WriteLine("Cancel request sent.");
            }
        }
    }
}
