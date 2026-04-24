using System;
using RabbitMqOrderService.Infrastructure;

namespace RabbitMqOrderService.Consumer
{
    /// <summary>
    /// Console host for <see cref="OrderServiceConsumer"/>.
    /// Replaces the WCF <c>ServiceHost</c> bootstrap in the original <c>Program.cs</c>.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("Starting RabbitMQ Order Service Consumer...");
            Console.WriteLine($"Connecting to {AppConfig.Host}:{AppConfig.Port} as {AppConfig.Username}");

            var factory = RabbitMqConnectionFactory.Create();

            using (var consumer = new OrderServiceConsumer(factory))
            {
                Console.WriteLine("Consumer running. Press ENTER to shut down.");
                Console.ReadLine();
                Console.WriteLine("Shutting down...");
            }

            Console.WriteLine("Consumer stopped.");
        }
    }
}
