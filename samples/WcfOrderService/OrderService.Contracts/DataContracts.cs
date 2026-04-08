using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OrderService.Contracts
{
    [DataContract(Namespace = "http://example.com/orders")]
    public class OrderRequest
    {
        [DataMember(IsRequired = true)]
        public string CustomerId { get; set; }

        [DataMember(IsRequired = true)]
        public List<OrderLine> Lines { get; set; }

        [DataMember]
        public string ShippingAddress { get; set; }
    }

    [DataContract(Namespace = "http://example.com/orders")]
    public class OrderLine
    {
        [DataMember(IsRequired = true)]
        public string ProductId { get; set; }

        [DataMember(IsRequired = true)]
        public int Quantity { get; set; }

        [DataMember(IsRequired = true)]
        public decimal UnitPrice { get; set; }
    }

    [DataContract(Namespace = "http://example.com/orders")]
    public class OrderConfirmation
    {
        [DataMember]
        public string OrderId { get; set; }

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public decimal TotalAmount { get; set; }
    }

    [DataContract(Namespace = "http://example.com/orders")]
    public class OrderStatus
    {
        [DataMember]
        public string OrderId { get; set; }

        [DataMember]
        public string Status { get; set; }   // Pending, Processing, Shipped, Delivered, Cancelled

        [DataMember]
        public string TrackingNumber { get; set; }
    }
}
