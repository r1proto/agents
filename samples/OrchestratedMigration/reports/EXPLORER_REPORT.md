# WCF Explorer Report
Generated: 2026-04-03
Solution Root: /home/runner/work/agents/agents/samples/WcfOrderService

## 1. Service Contracts

### IOrderService (http://example.com/orders)
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Contracts/IOrderService.cs

| Operation | Parameters | Return Type | IsOneWay | FaultContracts |
|---|---|---|---|---|
| PlaceOrder | OrderRequest request | OrderConfirmation | false | — (see note) |
| GetOrderStatus | string orderId | OrderStatus | false | — |
| CancelOrder | string orderId | void | true | — |

> **Note:** No `[FaultContract]` attributes are declared on the interface. However, `OrderServiceImpl.PlaceOrder` throws `FaultException<ValidationFault>` at runtime (for missing `CustomerId` or empty `Lines`). The contract and implementation are inconsistent on this point — a migration should add `[FaultContract(typeof(ValidationFault))]` to `PlaceOrder`.

---

## 2. Data Contracts

### OrderRequest (http://example.com/orders)
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Contracts/DataContracts.cs

| DataMember | CLR Type | Required | EmitDefault | Order | NameAlias |
|---|---|---|---|---|---|
| CustomerId | string | true | false | — | — |
| Lines | List\<OrderLine\> | true | false | — | — |
| ShippingAddress | string | false | false | — | — |

### OrderLine (http://example.com/orders)
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Contracts/DataContracts.cs

| DataMember | CLR Type | Required | EmitDefault | Order | NameAlias |
|---|---|---|---|---|---|
| ProductId | string | true | false | — | — |
| Quantity | int | true | false | — | — |
| UnitPrice | decimal | true | false | — | — |

### OrderConfirmation (http://example.com/orders)
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Contracts/DataContracts.cs

| DataMember | CLR Type | Required | EmitDefault | Order | NameAlias |
|---|---|---|---|---|---|
| OrderId | string | false | false | — | — |
| Success | bool | false | false | — | — |
| Message | string | false | false | — | — |
| TotalAmount | decimal | false | false | — | — |

### OrderStatus (http://example.com/orders)
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Contracts/DataContracts.cs

| DataMember | CLR Type | Required | EmitDefault | Order | NameAlias |
|---|---|---|---|---|---|
| OrderId | string | false | false | — | — |
| Status | string | false | false | — | — |
| TrackingNumber | string | false | false | — | — |

> **Status values (code comment):** Pending, Processing, Shipped, Delivered, Cancelled

---

## 3. Fault Types

### ValidationFault
File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService/ValidationFault.cs
Namespace attribute: `[DataContract]` — **no explicit namespace**; defaults to CLR-derived namespace.
CLR namespace: `OrderService`

| DataMember | CLR Type | Required | EmitDefault | Order | NameAlias |
|---|---|---|---|---|---|
| Field | string | false | false | — | — |
| Reason | string | false | false | — | — |

> **Usage:** Thrown as `FaultException<ValidationFault>` in `OrderServiceImpl.PlaceOrder` for two validation rules:
> 1. `Field="CustomerId"` — CustomerId is null or whitespace.
> 2. `Field="Lines"` — Lines collection is null or empty.

---

## 4. Bindings

### Server (OrderService/App.config)

| Service | Endpoint Address | Binding | BindingConfig | Security | Session | MaxMsgSize | Timeouts |
|---|---|---|---|---|---|---|---|
| OrderService.OrderServiceImpl | http://localhost:8080/orders | basicHttpBinding | SecureBinding | None | N/A | 65536 bytes | receiveTimeout=00:01:00, sendTimeout=00:01:00 |
| OrderService.OrderServiceImpl | http://localhost:8080/orders/mex | mexHttpBinding | (default) | N/A | N/A | — | — |

> Base address: `http://localhost:8080/orders` (defined in `<baseAddresses>`).

### Client (OrderService.Client/App.config)

| Endpoint Name | Address | Binding | BindingConfig | Security | MaxMsgSize | Timeouts |
|---|---|---|---|---|---|---|
| OrderServiceEndpoint | http://localhost:8080/orders | basicHttpBinding | OrderServiceBinding | None | 65536 bytes | receiveTimeout=00:01:00, sendTimeout=00:01:00 |

---

## 5. Service Behaviors

### Server Behaviors (OrderService/App.config)

| Name | Elements | Throttle Limits |
|---|---|---|
| OrderServiceBehavior | serviceMetadata(httpGetEnabled=true), serviceDebug(includeExceptionDetailInFaults=false), serviceThrottling | maxConcurrentCalls=16, maxConcurrentSessions=100, maxConcurrentInstances=100 |

> No client behaviors defined in OrderService.Client/App.config.

---

## 6. Client Proxies

| Class | File | Extends | Implements | Constructors | Methods |
|---|---|---|---|---|---|
| OrderServiceClient | /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Client/OrderServiceClient.cs | ClientBase\<IOrderService\> | IOrderService | default; (endpointConfigurationName); (endpointConfigurationName, remoteAddress) | PlaceOrder, GetOrderStatus, CancelOrder |

> All three methods delegate directly to `Channel.*` — no additional client-side logic, retries, or error handling.

---

## 7. Code Behaviors

| Class | File | Concern | Reproducible as middleware? |
|---|---|---|---|
| (none) | — | No `IServiceBehavior`, `IEndpointBehavior`, or `IOperationBehavior` implementations found in code. | N/A |

> The only behavioral configuration is declarative (App.config). No programmatic behavior extensions exist in this solution.

---

## 8. Special Handling Flags

- [ ] CallbackContract — no `CallbackContract` property on `[ServiceContract]`
- [ ] TransactionFlow — no `[TransactionFlow]` attribute on any operation
- [ ] Streaming (TransferMode.Streamed) — `basicHttpBinding` uses default Buffered transfer mode; no `transferMode` attribute set
- [ ] Message security (SecurityMode.Message) — both server and client bindings use `<security mode="None" />`
- [ ] Windows identity / impersonation — no `WindowsIdentity`, `ServiceSecurityContext`, or impersonation calls found
- [ ] PerSession instancing — no `[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]`; default is `PerCall`

---

## 9. Test Coverage Summary

File: /home/runner/work/agents/agents/samples/WcfOrderService/OrderService.Tests/OrderServiceTests.cs
Framework: MSTest (`[TestClass]`, `[TestMethod]`)
Test subject: `OrderServiceImpl` (direct instantiation — no WCF channel required)

| Test Method | Operation Under Test | Scenario |
|---|---|---|
| PlaceOrder_ValidRequest_ReturnsConfirmation | PlaceOrder | Happy path — checks Success=true, non-null OrderId, TotalAmount=20.00 |
| GetOrderStatus_AfterPlace_ReturnsPending | GetOrderStatus | Status is "Pending" immediately after PlaceOrder |
| CancelOrder_ExistingOrder_SetsStatusCancelled | CancelOrder + GetOrderStatus | Status becomes "Cancelled" after CancelOrder |
| PlaceOrder_NullCustomerId_ThrowsFault | PlaceOrder | Null CustomerId throws FaultException (ExpectedException) |

> Tests exercise `OrderServiceImpl` directly. No WCF transport or binding is exercised in tests.

---

## 10. Migration Notes

| Area | Observation |
|---|---|
| Transport | BasicHttpBinding with no security → maps cleanly to HTTP with no auth in target |
| Session | No session state; `PerCall` instancing; stateless except for static `_orders` dictionary |
| One-way op | `CancelOrder` is fire-and-forget; requires async void or publish-only producer in target |
| Fault contracts | `[FaultContract(typeof(ValidationFault))]` missing from interface; should be added before migration |
| Static dictionary | `_orders` is static — shared across all service instances; target must replace with real persistence |
| MEX endpoint | Metadata exchange endpoint present; not needed in target |
| Timeouts | Both send and receive set to 1 minute on both server and client |
