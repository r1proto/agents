using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrderService.Contracts;

namespace OrderService.Tests
{
    [TestClass]
    public class OrderServiceTests
    {
        private OrderServiceImpl _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new OrderServiceImpl();
        }

        [TestMethod]
        public void PlaceOrder_ValidRequest_ReturnsConfirmation()
        {
            var request = new OrderRequest
            {
                CustomerId = "CUST-001",
                ShippingAddress = "123 Main St",
                Lines = new List<OrderLine>
                {
                    new OrderLine { ProductId = "PROD-A", Quantity = 2, UnitPrice = 10.00m }
                }
            };

            var result = _service.PlaceOrder(request);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.OrderId);
            Assert.AreEqual(20.00m, result.TotalAmount);
        }

        [TestMethod]
        public void GetOrderStatus_AfterPlace_ReturnsPending()
        {
            var request = new OrderRequest
            {
                CustomerId = "CUST-002",
                Lines = new List<OrderLine>
                {
                    new OrderLine { ProductId = "PROD-B", Quantity = 1, UnitPrice = 50.00m }
                }
            };
            var confirmation = _service.PlaceOrder(request);

            var status = _service.GetOrderStatus(confirmation.OrderId);

            Assert.AreEqual("Pending", status.Status);
        }

        [TestMethod]
        public void CancelOrder_ExistingOrder_SetsStatusCancelled()
        {
            var request = new OrderRequest
            {
                CustomerId = "CUST-003",
                Lines = new List<OrderLine>
                {
                    new OrderLine { ProductId = "PROD-C", Quantity = 1, UnitPrice = 5.00m }
                }
            };
            var confirmation = _service.PlaceOrder(request);

            _service.CancelOrder(confirmation.OrderId);

            var status = _service.GetOrderStatus(confirmation.OrderId);
            Assert.AreEqual("Cancelled", status.Status);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ServiceModel.FaultException))]
        public void PlaceOrder_NullCustomerId_ThrowsFault()
        {
            var request = new OrderRequest
            {
                CustomerId = null,
                Lines = new List<OrderLine>
                {
                    new OrderLine { ProductId = "PROD-D", Quantity = 1, UnitPrice = 1.00m }
                }
            };
            _service.PlaceOrder(request);
        }
    }
}
