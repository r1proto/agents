using System;
using System.ServiceModel;

namespace OrderService
{
    /// <summary>
    /// Self-hosted WCF service host entry point.
    /// In production this would be hosted in IIS or a Windows Service.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(OrderServiceImpl)))
            {
                host.Open();
                Console.WriteLine("OrderService is running.");
                Console.WriteLine("Endpoint: http://localhost:8080/orders");
                Console.WriteLine("Press ENTER to stop.");
                Console.ReadLine();
                host.Close();
            }
        }
    }
}
