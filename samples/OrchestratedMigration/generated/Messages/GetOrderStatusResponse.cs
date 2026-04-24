using System;
using Newtonsoft.Json;

namespace OrchestratedMigration.Messages
{
    /// <summary>Reply DTO for the GetOrderStatus operation. Replaces WCF OrderStatus.</summary>
    public class GetOrderStatusResponse
    {
        /// <summary>Echo of the request CorrelationId for RPC matching.</summary>
        public Guid CorrelationId { get; set; }
        public string OrderId { get; set; }
        /// <summary>Valid values: Pending, Processing, Shipped, Delivered, Cancelled.</summary>
        public string Status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TrackingNumber { get; set; }
    }
}
