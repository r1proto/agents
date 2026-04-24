using System;
using OrchestratedMigration.Infrastructure;

namespace OrchestratedMigration.Consumer
{
    /// <summary>Console host for the OrderServiceConsumer.</summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("[Consumer Host] Starting OrderServiceConsumer...");

            var factory = RabbitMqConnectionFactory.Create();

            using (var consumer = new OrderServiceConsumer(factory))
            {
                Console.WriteLine("[Consumer Host] Listening. Press ENTER to shut down.");
                Console.ReadLine();
            }

            Console.WriteLine("[Consumer Host] Shut down cleanly.");
        }
    }
}
