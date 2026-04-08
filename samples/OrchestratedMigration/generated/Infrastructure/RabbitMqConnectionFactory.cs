using RabbitMQ.Client;

namespace OrchestratedMigration.Infrastructure
{
    /// <summary>Creates a pre-configured RabbitMQ ConnectionFactory from AppConfig.</summary>
    public static class RabbitMqConnectionFactory
    {
        /// <summary>Returns a configured <see cref="IConnectionFactory"/> ready to create connections.</summary>
        public static IConnectionFactory Create()
        {
            return new ConnectionFactory
            {
                HostName = AppConfig.Host,
                Port = AppConfig.Port,
                UserName = AppConfig.Username,
                Password = AppConfig.Password,
                VirtualHost = AppConfig.VirtualHost,
                AutomaticRecoveryEnabled = true
            };
        }
    }
}
