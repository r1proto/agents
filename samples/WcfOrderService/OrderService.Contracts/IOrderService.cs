using System.ServiceModel;

namespace OrderService.Contracts
{
    /// <summary>
    /// WCF service contract for order management.
    /// Clients call these operations synchronously over BasicHttpBinding.
    /// </summary>
    [ServiceContract(Namespace = "http://example.com/orders")]
    public interface IOrderService
    {
        /// <summary>
        /// Submit a new order. Returns an order confirmation with the assigned order ID.
        /// </summary>
        [OperationContract]
        OrderConfirmation PlaceOrder(OrderRequest request);

        /// <summary>
        /// Retrieve the current status of an existing order.
        /// </summary>
        [OperationContract]
        OrderStatus GetOrderStatus(string orderId);

        /// <summary>
        /// Cancel an order. One-way — no reply is expected.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void CancelOrder(string orderId);
    }
}
