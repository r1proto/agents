using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMqOrderService.Consumer;
using RabbitMqOrderService.Messages;

namespace RabbitMqOrderService.Tests
{
    /// <summary>
    /// Unit tests for <see cref="OrderLogic"/>.
    /// Mirrors the four WCF tests in <c>OrderService.Tests/OrderServiceTests.cs</c> but exercises
    /// the pure business logic without any RabbitMQ dependency.
    /// </summary>
    [TestClass]
    public class OrderServiceConsumerTests
    {
        private OrderLogic _logic;

        /// <summary>Creates a fresh <see cref="OrderLogic"/> instance before each test.</summary>
        [TestInitialize]
        public void Setup()
        {
            _logic = new OrderLogic();
        }

        // ── PlaceOrder ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors WCF test: <c>PlaceOrder_ValidRequest_ReturnsConfirmation</c>.
        /// A well-formed request must return a successful response with the correct total.
        /// </summary>
        [TestMethod]
        public void PlaceOrder_ValidRequest_ReturnsSuccessfulResponse()
        {
            var msg = new PlaceOrderMessage
            {
                CustomerId      = "CUST-001",
                ShippingAddress = "123 Main St",
                Lines = new List<OrderLineDto>
                {
                    new OrderLineDto { ProductId = "PROD-A", Quantity = 2, UnitPrice = 10.00m }
                }
            };

            var response = _logic.HandlePlaceOrder(msg);

            Assert.IsTrue(response.Success, "Expected Success=true");
            Assert.IsNotNull(response.OrderId, "Expected a non-null OrderId");
            Assert.AreEqual(20.00m, response.TotalAmount, "Expected TotalAmount = 2 × $10.00 = $20.00");
        }

        // ── GetOrderStatus ────────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors WCF test: <c>GetOrderStatus_AfterPlace_ReturnsPending</c>.
        /// A newly placed order must have status "Pending".
        /// </summary>
        [TestMethod]
        public void GetOrderStatus_AfterPlace_ReturnsPending()
        {
            var placeMsg = new PlaceOrderMessage
            {
                CustomerId = "CUST-002",
                Lines = new List<OrderLineDto>
                {
                    new OrderLineDto { ProductId = "PROD-B", Quantity = 1, UnitPrice = 50.00m }
                }
            };
            var placeResp = _logic.HandlePlaceOrder(placeMsg);
            Assert.IsTrue(placeResp.Success);

            var (statusResp, error) = _logic.HandleGetOrderStatus(
                new GetOrderStatusMessage { OrderId = placeResp.OrderId });

            Assert.IsNull(error,  $"Unexpected error: {error?.Reason}");
            Assert.IsNotNull(statusResp);
            Assert.AreEqual("Pending", statusResp.Status);
        }

        // ── CancelOrder ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors WCF test: <c>CancelOrder_ExistingOrder_SetsStatusCancelled</c>.
        /// After cancellation the status must change to "Cancelled".
        /// </summary>
        [TestMethod]
        public void CancelOrder_ExistingOrder_SetsStatusCancelled()
        {
            var placeMsg = new PlaceOrderMessage
            {
                CustomerId = "CUST-003",
                Lines = new List<OrderLineDto>
                {
                    new OrderLineDto { ProductId = "PROD-C", Quantity = 1, UnitPrice = 5.00m }
                }
            };
            var placeResp = _logic.HandlePlaceOrder(placeMsg);
            Assert.IsTrue(placeResp.Success);

            _logic.HandleCancelOrder(new CancelOrderMessage { OrderId = placeResp.OrderId });

            var (statusResp, error) = _logic.HandleGetOrderStatus(
                new GetOrderStatusMessage { OrderId = placeResp.OrderId });

            Assert.IsNull(error, $"Unexpected error: {error?.Reason}");
            Assert.IsNotNull(statusResp);
            Assert.AreEqual("Cancelled", statusResp.Status);
        }

        // ── Validation ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors WCF test: <c>PlaceOrder_NullCustomerId_ThrowsFault</c>.
        /// A missing CustomerId must produce a non-successful response (replaces FaultException).
        /// </summary>
        [TestMethod]
        public void PlaceOrder_NullCustomerId_ReturnsFailureResponse()
        {
            var msg = new PlaceOrderMessage
            {
                CustomerId = null,
                Lines = new List<OrderLineDto>
                {
                    new OrderLineDto { ProductId = "PROD-D", Quantity = 1, UnitPrice = 1.00m }
                }
            };

            var response = _logic.HandlePlaceOrder(msg);

            Assert.IsFalse(response.Success, "Expected Success=false for null CustomerId");
            StringAssert.Contains(response.Message, "CustomerId");
        }
    }
}
