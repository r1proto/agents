# WCF → RabbitMQ Migration Plan
Generated from: `EXPLORER_REPORT.md` (Solution Root: `samples/WcfOrderService`)
Messaging library preference: **raw `RabbitMQ.Client`** (no MassTransit)

---

## Section 1 — Concept Mapping Table

| WCF Construct | WCF Location | RabbitMQ Equivalent | Exchange | Queue / Routing Key | Notes |
|---|---|---|---|---|---|
| `[ServiceContract] IOrderService` | `OrderService.Contracts/IOrderService.cs` | `OrderServiceConsumer` worker + `OrderServiceRabbitMqClient` | `order-service` (direct) | queue: `order-service` | Single exchange/queue covers all operations via routing key |
| `[OperationContract] PlaceOrder` (request-reply) | `OrderService.Contracts/IOrderService.cs` | Publish `PlaceOrderMessage`, consume `PlaceOrderResponse` on reply queue | `order-service` | routing key: `place-order` | Correlation via `CorrelationId` + `ReplyTo` fields |
| `[OperationContract] GetOrderStatus` (request-reply) | `OrderService.Contracts/IOrderService.cs` | Publish `GetOrderStatusMessage`, consume `GetOrderStatusResponse` on reply queue | `order-service` | routing key: `get-order-status` | Correlation via `CorrelationId` + `ReplyTo` fields |
| `[OperationContract] CancelOrder` (one-way) | `OrderService.Contracts/IOrderService.cs` | Fire-and-forget publish `CancelOrderMessage` | `order-service` | routing key: `cancel-order` | No reply queue; `IsOneWay=true` maps to publish-only |
| `[DataContract] OrderRequest` | `OrderService.Contracts/DataContracts.cs` | `PlaceOrderMessage` DTO | — | — | Contains nested `OrderLineDto` list |
| `[DataContract] OrderLine` | `OrderService.Contracts/DataContracts.cs` | `OrderLineDto` (nested class in `PlaceOrderMessage`) | — | — | Kept as nested for co-location |
| `[DataContract] OrderConfirmation` | `OrderService.Contracts/DataContracts.cs` | `PlaceOrderResponse` DTO | — | — | Adds `Success` flag already present in original |
| `[DataContract] OrderStatus` | `OrderService.Contracts/DataContracts.cs` | `GetOrderStatusResponse` DTO | — | — | Status string values (Pending, Processing, Shipped, Delivered, Cancelled) preserved |
| `ValidationFault` | `OrderService/ValidationFault.cs` | `ErrorReplyMessage` DTO | — | — | Returned on reply queue instead of thrown as `FaultException<T>` |
| `ServiceHost + OrderServiceImpl` | `OrderService/OrderServiceImpl.cs`, `OrderService/Program.cs` | `OrderServiceConsumer` (EventingBasicConsumer worker) | — | — | Static `_orders` dict must be replaced with real persistence |
| `OrderServiceClient (ClientBase<IOrderService>)` | `OrderService.Client/OrderServiceClient.cs` | `OrderServiceRabbitMqClient` | — | — | RPC calls (PlaceOrder, GetOrderStatus) use correlation-id pattern; CancelOrder is fire-and-forget |

---

## Section 2 — Schema Comparison Table

| WCF Type | WCF Field | WCF CLR Type | Required? | EmitDefault? | → | RabbitMQ DTO | RabbitMQ Field | RabbitMQ CLR Type | Safe? | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| `OrderRequest` | `CustomerId` | `string` | true | false | → | `PlaceOrderMessage` | `CustomerId` | `string` | ✅ | Unchanged |
| `OrderRequest` | `Lines` | `List<OrderLine>` | true | false | → | `PlaceOrderMessage` | `Lines` | `List<OrderLineDto>` | ⚠️ | Type renamed to `OrderLineDto`; structure identical |
| `OrderRequest` | `ShippingAddress` | `string` | false | false | → | `PlaceOrderMessage` | `ShippingAddress` | `string` | ✅ | Unchanged; nullable |
| `OrderRequest` | _(added)_ | — | — | — | → | `PlaceOrderMessage` | `CorrelationId` | `Guid` | ✅ | Envelope field for RPC correlation |
| `OrderRequest` | _(added)_ | — | — | — | → | `PlaceOrderMessage` | `ReplyTo` | `string` | ✅ | Envelope field; reply queue name |
| `OrderLine` | `ProductId` | `string` | true | false | → | `OrderLineDto` | `ProductId` | `string` | ✅ | Unchanged |
| `OrderLine` | `Quantity` | `int` | true | false | → | `OrderLineDto` | `Quantity` | `int` | ✅ | Unchanged |
| `OrderLine` | `UnitPrice` | `decimal` | true | false | → | `OrderLineDto` | `UnitPrice` | `decimal` | ✅ | Unchanged; verify JSON decimal precision |
| `OrderConfirmation` | `OrderId` | `string` | false | false | → | `PlaceOrderResponse` | `OrderId` | `string` | ✅ | Unchanged |
| `OrderConfirmation` | `Success` | `bool` | false | false | → | `PlaceOrderResponse` | `Success` | `bool` | ✅ | Unchanged; clients must check this flag |
| `OrderConfirmation` | `Message` | `string` | false | false | → | `PlaceOrderResponse` | `Message` | `string` | ✅ | Unchanged |
| `OrderConfirmation` | `TotalAmount` | `decimal` | false | false | → | `PlaceOrderResponse` | `TotalAmount` | `decimal` | ✅ | Unchanged |
| `OrderConfirmation` | _(added)_ | — | — | — | → | `PlaceOrderResponse` | `CorrelationId` | `Guid` | ✅ | Echo of request CorrelationId for RPC matching |
| `OrderStatus` | `OrderId` | `string` | false | false | → | `GetOrderStatusResponse` | `OrderId` | `string` | ✅ | Unchanged |
| `OrderStatus` | `Status` | `string` | false | false | → | `GetOrderStatusResponse` | `Status` | `string` | ✅ | Valid values: Pending, Processing, Shipped, Delivered, Cancelled |
| `OrderStatus` | `TrackingNumber` | `string` | false | false | → | `GetOrderStatusResponse` | `TrackingNumber` | `string` | ✅ | Unchanged; nullable |
| `OrderStatus` | _(added)_ | — | — | — | → | `GetOrderStatusResponse` | `CorrelationId` | `Guid` | ✅ | Echo of request CorrelationId for RPC matching |
| _(GetOrderStatus param)_ | `orderId` | `string` | true | — | → | `GetOrderStatusMessage` | `OrderId` | `string` | ✅ | Promoted from method parameter to DTO field |
| `GetOrderStatusMessage` | _(added)_ | — | — | — | → | `GetOrderStatusMessage` | `CorrelationId` | `Guid` | ✅ | Envelope field for RPC correlation |
| `GetOrderStatusMessage` | _(added)_ | — | — | — | → | `GetOrderStatusMessage` | `ReplyTo` | `string` | ✅ | Envelope field; reply queue name |
| _(CancelOrder param)_ | `orderId` | `string` | true | — | → | `CancelOrderMessage` | `OrderId` | `string` | ✅ | Promoted from method parameter to DTO field; no CorrelationId/ReplyTo (one-way) |
| `ValidationFault` | `Field` | `string` | false | false | → | `ErrorReplyMessage` | `Field` | `string` | ✅ | Unchanged |
| `ValidationFault` | `Reason` | `string` | false | false | → | `ErrorReplyMessage` | `Reason` | `string` | ✅ | Unchanged |
| `ValidationFault` | _(added)_ | — | — | — | → | `ErrorReplyMessage` | `CorrelationId` | `Guid` | ✅ | Echo of request CorrelationId so client can match error to request |

---

## Section 3 — File Manifest

All files to be created under `samples/OrchestratedMigration/generated/`:

| File Path | Purpose | Replaces |
|---|---|---|
| `Messages/PlaceOrderMessage.cs` | Request DTO for PlaceOrder (includes `OrderLineDto` nested class) | `OrderRequest` + `OrderLine` |
| `Messages/PlaceOrderResponse.cs` | Reply DTO for PlaceOrder | `OrderConfirmation` |
| `Messages/GetOrderStatusMessage.cs` | Request DTO for GetOrderStatus | `GetOrderStatus` method parameter |
| `Messages/GetOrderStatusResponse.cs` | Reply DTO for GetOrderStatus | `OrderStatus` |
| `Messages/CancelOrderMessage.cs` | One-way fire-and-forget message DTO | `CancelOrder` method parameter |
| `Messages/ErrorReplyMessage.cs` | Error reply DTO (returned on reply queue) | `ValidationFault` / `FaultException<ValidationFault>` |
| `Infrastructure/AppConfig.cs` | Reads RabbitMQ settings from `appSettings` (host, port, user, password, vhost) | — |
| `Infrastructure/RabbitMqConnectionFactory.cs` | Creates `ConnectionFactory` from `AppConfig`; returns `IConnection` | — |
| `Consumer/OrderServiceConsumer.cs` | Consumer worker using `EventingBasicConsumer`; dispatches on routing key | `ServiceHost` + `OrderServiceImpl` |
| `Consumer/Program.cs` | Console host that wires up `RabbitMqConnectionFactory` and `OrderServiceConsumer` | `OrderService/Program.cs` |
| `Client/OrderServiceRabbitMqClient.cs` | RPC publisher (PlaceOrder, GetOrderStatus) + fire-and-forget (CancelOrder) | `OrderServiceClient (ClientBase<IOrderService>)` |
| `Client/Program.cs` | Demo client that exercises all three operations | `OrderService.Client/Program.cs` |
| `Verification/WcfVerificationClient.cs` | Calls the original WCF service for each operation to capture baseline results | — |
| `Verification/RabbitMqVerificationClient.cs` | Sends identical scenarios to the RabbitMQ service and compares with baseline | — |
| `Config/App.config` | RabbitMQ `appSettings` (host, port, username, password, virtualHost, exchangeName, queueName) | `OrderService/App.config` + `OrderService.Client/App.config` |
| `Tests/OrderLogic.cs` | Extracted pure business logic (order storage, validation, total calculation) for unit testing | _(extracted from `OrderServiceImpl`)_ |
| `Tests/OrderServiceConsumerTests.cs` | MSTest unit tests mirroring the original 4 WCF tests against `OrderLogic` | `OrderService.Tests/OrderServiceTests.cs` |
| `MIGRATION_REPORT.md` | Before/after reference document written by the Verifier agent | — |

---

## Section 4 — NuGet Packages

| Package | Version | Reason |
|---|---|---|
| `RabbitMQ.Client` | 6.8.1 | Core AMQP 0-9-1 connectivity; `EventingBasicConsumer`, `IModel`, `ConnectionFactory` |
| `Newtonsoft.Json` | 13.0.3 | DTO serialization / deserialization for message bodies |

---

## Section 5 — Behavioral Differences

| # | Aspect | WCF Behavior | RabbitMQ Behavior | Risk Level | Mitigation |
|---|---|---|---|---|---|
| 1 | Delivery guarantee | At-most-once (HTTP request/response; no retry on failure) | At-least-once (manual `BasicAck`; broker re-queues on consumer crash) | Medium | Make all consumer handlers idempotent; use `OrderId` as idempotency key |
| 2 | Synchrony | Blocking synchronous call; caller blocks thread until response received | `async Task<T>` with `TaskCompletionSource` callback keyed on `CorrelationId` | High | Refactor all callers to `async/await`; set explicit RPC timeout to match original 1-minute `sendTimeout` |
| 3 | Error model | `FaultException<ValidationFault>` thrown and propagated over SOAP fault channel | `ErrorReplyMessage` published on reply queue; no exception crosses the wire | Medium | Clients must inspect `Success` flag (or a discriminated reply type) before consuming response fields |
| 4 | Security | `basicHttpBinding` with `<security mode="None"/>` — no transport or message security | Broker credentials (`username`/`password`) + optional TLS (`Ssl` property on `ConnectionFactory`) | Low | Add RabbitMQ credentials to `Config/App.config`; enable TLS for non-local environments |
| 5 | Message ordering | Preserved per HTTP connection (single TCP stream per client) | Not guaranteed across multiple consumers or when prefetch > 1 | Low | Use a single consumer instance per queue if strict ordering is required; document the relaxation |
| 6 | Service throttling | `serviceThrottling`: `maxConcurrentCalls=16`, `maxConcurrentSessions=100`, `maxConcurrentInstances=100` | QoS `prefetchCount` on the channel controls in-flight message count | Low | Call `channel.BasicQos(0, 16, false)` to mirror `maxConcurrentCalls=16` |

---

## Section 6 — Special Pattern Decisions

The Explorer report raised **no special handling flags**:

- No `CallbackContract` (duplex) — no special pattern needed.
- No `[TransactionFlow]` — no distributed transaction handling needed.
- No streaming (`TransferMode.Streamed`) — all messages fit in default buffer.
- No message security (`SecurityMode.Message`) — no signing/encryption layer needed.
- No Windows identity / impersonation — no principal propagation needed.
- No `PerSession` instancing — service is already effectively stateless per call (except for the shared static `_orders` dictionary, which is an implementation detail, not a WCF session concern).

> This section is intentionally empty; no special patterns require custom handling in the generated code.

---

## Approval Gate

```
Status: APPROVED
[x] User has reviewed and approved this plan.
    Approved by: ___________  Date: ___________
```
