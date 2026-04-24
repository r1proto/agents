using System.Configuration;

namespace OrchestratedMigration.Infrastructure
{
    /// <summary>Reads RabbitMQ connection settings from appSettings.</summary>
    public static class AppConfig
    {
        public static string Host => ConfigurationManager.AppSettings["RabbitMq:Host"] ?? "localhost";
        public static int Port => int.TryParse(ConfigurationManager.AppSettings["RabbitMq:Port"], out var p) ? p : 5672;
        public static string Username => ConfigurationManager.AppSettings["RabbitMq:Username"] ?? "guest";
        public static string Password => ConfigurationManager.AppSettings["RabbitMq:Password"] ?? "guest";
        public static string VirtualHost => ConfigurationManager.AppSettings["RabbitMq:VirtualHost"] ?? "/";
    }
}
