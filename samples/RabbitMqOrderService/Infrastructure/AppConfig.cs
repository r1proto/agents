using System;
using System.Configuration;

namespace RabbitMqOrderService.Infrastructure
{
    /// <summary>
    /// Reads RabbitMQ connection settings from <c>App.config</c> / <c>Web.config</c> appSettings.
    /// Falls back to sensible defaults so the application starts without any configuration.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>RabbitMQ broker hostname (default: <c>localhost</c>).</summary>
        public static string Host =>
            ConfigurationManager.AppSettings["RabbitMq:Host"] ?? "localhost";

        /// <summary>AMQP port (default: <c>5672</c>).</summary>
        public static int Port
        {
            get
            {
                var raw = ConfigurationManager.AppSettings["RabbitMq:Port"];
                return int.TryParse(raw, out var port) ? port : 5672;
            }
        }

        /// <summary>Broker username (default: <c>guest</c>).</summary>
        public static string Username =>
            ConfigurationManager.AppSettings["RabbitMq:Username"] ?? "guest";

        /// <summary>Broker password (default: <c>guest</c>).</summary>
        public static string Password =>
            ConfigurationManager.AppSettings["RabbitMq:Password"] ?? "guest";

        /// <summary>AMQP virtual host (default: <c>/</c>).</summary>
        public static string VirtualHost =>
            ConfigurationManager.AppSettings["RabbitMq:VirtualHost"] ?? "/";
    }
}
