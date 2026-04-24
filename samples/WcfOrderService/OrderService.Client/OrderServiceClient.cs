using System.ServiceModel;
using OrderService.Contracts;

namespace OrderService.Client
{
    /// <summary>
    /// WCF client proxy for IOrderService.
    /// Generated pattern: inherits ClientBase&lt;T&gt; and exposes the service contract methods.
    /// </summary>
    public class OrderServiceClient : ClientBase<IOrderService>, IOrderService
    {
        public OrderServiceClient() { }

        public OrderServiceClient(string endpointConfigurationName)
            : base(endpointConfigurationName) { }

        public OrderServiceClient(string endpointConfigurationName, string remoteAddress)
            : base(endpointConfigurationName, remoteAddress) { }

        public OrderConfirmation PlaceOrder(OrderRequest request)
            => Channel.PlaceOrder(request);

        public OrderStatus GetOrderStatus(string orderId)
            => Channel.GetOrderStatus(orderId);

        public void CancelOrder(string orderId)
            => Channel.CancelOrder(orderId);
    }
}
