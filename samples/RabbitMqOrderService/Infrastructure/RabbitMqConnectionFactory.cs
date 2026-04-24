using RabbitMQ.Client;

namespace RabbitMqOrderService.Infrastructure
{
    /// <summary>
    /// Creates a configured <see cref="IConnectionFactory"/> from values in <see cref="AppConfig"/>.
    /// Centralises all broker connection settings in one place.
    /// </summary>
    public static class RabbitMqConnectionFactory
    {
        /// <summary>
        /// Builds and returns a <see cref="ConnectionFactory"/> pre-populated with
        /// host, port, credentials, and virtual-host read from <see cref="AppConfig"/>.
        /// </summary>
        /// <returns>A ready-to-use <see cref="IConnectionFactory"/>.</returns>
        public static IConnectionFactory Create()
        {
            return new ConnectionFactory
            {
                HostName    = AppConfig.Host,
                Port        = AppConfig.Port,
                UserName    = AppConfig.Username,
                Password    = AppConfig.Password,
                VirtualHost = AppConfig.VirtualHost,
                // Re-create topology on reconnect (requires the Automatic Recovery feature).
                AutomaticRecoveryEnabled = true
            };
        }
    }
}
