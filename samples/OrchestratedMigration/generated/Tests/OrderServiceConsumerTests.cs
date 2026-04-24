using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrchestratedMigration.Tests
{
    /// <summary>
    /// MSTest unit tests for <see cref="OrderLogic"/>.
    /// Mirrors the 4 original WCF service tests but targets the pure business logic class.
    /// </summary>
    [TestClass]
    public class OrderServiceConsumerTests
    {
        private OrderLogic _logic;

        [TestInitialize]
        public void Setup()
        {
            _logic = new OrderLogic();
        }

        // ── Test 1 ───────────────────────────────────────────────────────────────

        /// <summary>PlaceOrder with a valid request returns a successful confirmation.</summary>
        [TestMethod]
        public void PlaceOrder_ValidRequest_ReturnsConfirmation()
        {
            var lines = new List<(string ProductId, int Qty, decimal Price)>
            {
                ("PROD-A", 2, 10.00m)
            };

            var result = _logic.PlaceOrder("CUST-001", lines);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.OrderId);
            Assert.AreEqual(20.00m, result.TotalAmount);
        }

        // ── Test 2 ───────────────────────────────────────────────────────────────

        /// <summary>GetOrderStatus immediately after PlaceOrder returns "Pending".</summary>
        [TestMethod]
        public void GetOrderStatus_AfterPlace_ReturnsPending()
        {
            var lines = new List<(string ProductId, int Qty, decimal Price)>
            {
                ("PROD-A", 1, 10.00m)
            };

            var confirmation = _logic.PlaceOrder("CUST-002", lines);
            var record = _logic.GetOrderStatus(confirmation.OrderId);

            Assert.AreEqual("Pending", record.Status);
        }

        // ── Test 3 ───────────────────────────────────────────────────────────────

        /// <summary>CancelOrder on an existing order sets its status to "Cancelled".</summary>
        [TestMethod]
        public void CancelOrder_ExistingOrder_SetsStatusCancelled()
        {
            var lines = new List<(string ProductId, int Qty, decimal Price)>
            {
                ("PROD-B", 3, 5.00m)
            };

            var confirmation = _logic.PlaceOrder("CUST-003", lines);
            _logic.CancelOrder(confirmation.OrderId);

            var record = _logic.GetOrderStatus(confirmation.OrderId);
            Assert.AreEqual("Cancelled", record.Status);
        }

        // ── Test 4 ───────────────────────────────────────────────────────────────

        /// <summary>PlaceOrder with a null CustomerId throws ArgumentException.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PlaceOrder_NullCustomerId_ThrowsException()
        {
            var lines = new List<(string ProductId, int Qty, decimal Price)>
            {
                ("PROD-C", 1, 9.99m)
            };

            _logic.PlaceOrder(null, lines);
        }
    }
}
