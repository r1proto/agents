# WCF → RabbitMQ Migration Report
**Service:** `IOrderService` / `OrderServiceImpl`  
**Original transport:** WCF `basicHttpBinding` (HTTP, no security)  
**Target transport:** RabbitMQ `direct` exchange — raw `RabbitMQ.Client` 6.8.1  
**Date:** 2026-04-03  
**Status:** ✅ Generated — pending build + test validation

---

## Section 1 — Structural Before/After Table

| # | WCF Construct | WCF File | RabbitMQ Replacement | New File | Change Summary |
|---|---|---|---|---|---|
| 1 | `[ServiceContract] IOrderService` | `OrderService.Contracts/IOrderService.cs` | `OrderServiceConsumer` worker + `OrderServiceRabbitMqClient` | `Consumer/OrderServiceConsumer.cs`, `Client/OrderServiceRabbitMqClient.cs` | Interface replaced by a durable direct exchange (`order-service`); all three operations dispatched by routing key |
| 2 | `[OperationContract] PlaceOrder` (request-reply) | `OrderService.Contracts/IOrderService.cs` | Publish `PlaceOrderMessage` → routing key `place-order`; await `PlaceOrderResponse` on exclusive reply queue | `Messages/PlaceOrderMessage.cs`, `Messages/PlaceOrderResponse.cs` | Correlation-id RPC pattern replaces blocking SOAP call; `async Task<PlaceOrderResponse>` |
| 3 | `[OperationContract] GetOrderStatus` (request-reply) | `OrderService.Contracts/IOrderService.cs` | Publish `GetOrderStatusMessage` → routing key `get-order-status`; await `GetOrderStatusResponse` | `Messages/GetOrderStatusMessage.cs`, `Messages/GetOrderStatusResponse.cs` | Same correlation-id RPC pattern |
| 4 | `[OperationContract] CancelOrder` (one-way) | `OrderService.Contracts/IOrderService.cs` | Fire-and-forget publish `CancelOrderMessage` → routing key `cancel-order` | `Messages/CancelOrderMessage.cs` | `IsOneWay=true` maps to publish-only; no reply queue, no correlation wait |
| 5 | `[DataContract] OrderRequest` | `OrderService.Contracts/DataContracts.cs` | `PlaceOrderMessage` | `Messages/PlaceOrderMessage.cs` | Fields identical; two envelope fields added (`CorrelationId`, `ReplyTo`) |
| 6 | `[DataContract] OrderLine` | `OrderService.Contracts/DataContracts.cs` | `OrderLineDto` (nested in `PlaceOrderMessage.cs`) | `Messages/PlaceOrderMessage.cs` | Renamed; structure identical; co-located with parent |
| 7 | `[DataContract] OrderConfirmation` | `OrderService.Contracts/DataContracts.cs` | `PlaceOrderResponse` | `Messages/PlaceOrderResponse.cs` | Fields identical; `CorrelationId` added for RPC matching |
| 8 | `[DataContract] OrderStatus` | `OrderService.Contracts/DataContracts.cs` | `GetOrderStatusResponse` | `Messages/GetOrderStatusResponse.cs` | Fields identical; `CorrelationId` added; `TrackingNumber` decorated with `[JsonProperty(NullValueHandling.Ignore)]` |
| 9 | `ValidationFault` | `OrderService/ValidationFault.cs` | `ErrorReplyMessage` | `Messages/ErrorReplyMessage.cs` | No longer thrown as `FaultException<T>`; returned as a plain message on the reply queue; `ErrorCode` field added |
| 10 | `ServiceHost + OrderServiceImpl` | `OrderService/OrderServiceImpl.cs`, `OrderService/Program.cs` | `OrderServiceConsumer` + `Consumer/Program.cs` | `Consumer/OrderServiceConsumer.cs`, `Consumer/Program.cs` | `ServiceHost` replaced by `EventingBasicConsumer`; business logic preserved; `BasicQos(prefetchCount:1)` mirrors WCF throttling |
| 11 | `OrderServiceClient (ClientBase<IOrderService>)` | `OrderService.Client/OrderServiceClient.cs` | `OrderServiceRabbitMqClient` | `Client/OrderServiceRabbitMqClient.cs` | `Channel.*` proxy calls replaced by publish/await pattern using `ConcurrentDictionary<corrId, TCS>`; `CancelOrder` is fire-and-forget |
| 12 | `OrderService/App.config` (WCF server config) | `OrderService/App.config` | `Config/App.config` (RabbitMQ settings) | `Config/App.config` | `<system.serviceModel>` block replaced by `<appSettings>` keys: host, port, username, password, virtualHost, exchangeName, queueName |
| 13 | `OrderService.Client/App.config` (WCF client config) | `OrderService.Client/App.config` | merged into `Config/App.config` | `Config/App.config` | Separate client config eliminated; single shared config for consumer and client |
| 14 | `OrderService.Tests/OrderServiceTests.cs` | `OrderService.Tests/OrderServiceTests.cs` | `Tests/OrderServiceConsumerTests.cs` over extracted `OrderLogic` | `Tests/OrderLogic.cs`, `Tests/OrderServiceConsumerTests.cs` | 4 MSTest methods ported; WCF transport removed from test path; pure unit tests against `OrderLogic` |

---

## Section 2 — Schema Comparison Table (Finalised)

Reflects actual fields found in generated files after Executor completed work.

| WCF Type | WCF Field | WCF CLR Type | Req? | → | RabbitMQ DTO | RabbitMQ Field | RabbitMQ CLR Type | Safe? | Notes |
|---|---|---|---|---|---|---|---|---|---|
| `OrderRequest` | `CustomerId` | `string` | ✔ | → | `PlaceOrderMessage` | `CustomerId` | `string` | ✅ | Unchanged |
| `OrderRequest` | `Lines` | `List<OrderLine>` | ✔ | → | `PlaceOrderMessage` | `Lines` | `List<OrderLineDto>` | ⚠️ | Type renamed to `OrderLineDto`; structure identical |
| `OrderRequest` | `ShippingAddress` | `string` | — | → | `PlaceOrderMessage` | `ShippingAddress` | `string` | ✅ | Unchanged; nullable |
| _(added)_ | — | — | — | → | `PlaceOrderMessage` | `CorrelationId` | `Guid` | ✅ | Envelope field; auto-set to `Guid.NewGuid()` |
| _(added)_ | — | — | — | → | `PlaceOrderMessage` | `ReplyTo` | `string` | ✅ | Envelope field; reply queue name set by client |
| `OrderLine` | `ProductId` | `string` | ✔ | → | `OrderLineDto` | `ProductId` | `string` | ✅ | Unchanged |
| `OrderLine` | `Quantity` | `int` | ✔ | → | `OrderLineDto` | `Quantity` | `int` | ✅ | Unchanged |
| `OrderLine` | `UnitPrice` | `decimal` | ✔ | → | `OrderLineDto` | `UnitPrice` | `decimal` | ✅ | Unchanged; verify JSON decimal precision in edge cases |
| `OrderConfirmation` | `OrderId` | `string` | — | → | `PlaceOrderResponse` | `OrderId` | `string` | ✅ | Unchanged |
| `OrderConfirmation` | `Success` | `bool` | — | → | `PlaceOrderResponse` | `Success` | `bool` | ✅ | Clients must check this flag before consuming other fields |
| `OrderConfirmation` | `Message` | `string` | — | → | `PlaceOrderResponse` | `Message` | `string` | ✅ | Unchanged |
| `OrderConfirmation` | `TotalAmount` | `decimal` | — | → | `PlaceOrderResponse` | `TotalAmount` | `decimal` | ✅ | Unchanged |
| _(added)_ | — | — | — | → | `PlaceOrderResponse` | `CorrelationId` | `Guid` | ✅ | Echo of request CorrelationId for RPC matching |
| _(GetOrderStatus param)_ | `orderId` | `string` | ✔ | → | `GetOrderStatusMessage` | `OrderId` | `string` | ✅ | Promoted from method parameter to DTO field |
| _(added)_ | — | — | — | → | `GetOrderStatusMessage` | `CorrelationId` | `Guid` | ✅ | Auto-set to `Guid.NewGuid()` |
| _(added)_ | — | — | — | → | `GetOrderStatusMessage` | `ReplyTo` | `string` | ✅ | Set by client before publish |
| `OrderStatus` | `OrderId` | `string` | — | → | `GetOrderStatusResponse` | `OrderId` | `string` | ✅ | Unchanged |
| `OrderStatus` | `Status` | `string` | — | → | `GetOrderStatusResponse` | `Status` | `string` | ✅ | Values: Pending, Processing, Shipped, Delivered, Cancelled |
| `OrderStatus` | `TrackingNumber` | `string` | — | → | `GetOrderStatusResponse` | `TrackingNumber` | `string` | ✅ | `[JsonProperty(NullValueHandling.Ignore)]` added |
| _(added)_ | — | — | — | → | `GetOrderStatusResponse` | `CorrelationId` | `Guid` | ✅ | Echo for RPC matching |
| _(CancelOrder param)_ | `orderId` | `string` | ✔ | → | `CancelOrderMessage` | `OrderId` | `string` | ✅ | Promoted from parameter to DTO field |
| _(added — deviation)_ | — | — | — | → | `CancelOrderMessage` | `CorrelationId` | `Guid` | ✅ | **Executor addition** (plan said omit); used for tracing only; no ReplyTo |
| `ValidationFault` | `Field` | `string` | — | → | `ErrorReplyMessage` | `Field` | `string` | ✅ | Unchanged |
| `ValidationFault` | `Reason` | `string` | — | → | `ErrorReplyMessage` | `Reason` | `string` | ✅ | Unchanged |
| _(added)_ | — | — | — | → | `ErrorReplyMessage` | `CorrelationId` | `Guid` | ✅ | Echo for client error matching |
| _(added — deviation)_ | — | — | — | → | `ErrorReplyMessage` | `ErrorCode` | `string` | ✅ | **Executor addition** (not in plan); provides structured error codes |

---

## Section 3 — Behavioral Differences

| # | Aspect | WCF Behavior | RabbitMQ Behavior | Risk Level | Mitigation |
|---|---|---|---|---|---|
| 1 | Delivery guarantee | At-most-once (HTTP request/response; no retry on failure) | At-least-once (`BasicAck` after handler completes; broker re-queues on consumer crash) | Medium | Make all consumer handlers idempotent; use `OrderId` as idempotency key |
| 2 | Synchrony | Blocking synchronous call; caller blocks thread until response received | `async Task<T>` with `TaskCompletionSource` callback keyed on `CorrelationId` | High | Refactor all callers to `async/await`; set explicit RPC timeout matching original 1-minute `sendTimeout` |
| 3 | Error model | `FaultException<ValidationFault>` thrown and propagated over SOAP fault channel | `ErrorReplyMessage` published on reply queue; no exception crosses the wire | Medium | Clients must inspect `Success` flag (or check for `ErrorReplyMessage`) before consuming response fields |
| 4 | Security | `basicHttpBinding` with `<security mode="None"/>` — no transport or message security | Broker credentials (`username`/`password`) in `App.config`; optional TLS via `Ssl` on `ConnectionFactory` | Low | Add RabbitMQ credentials to `Config/App.config`; enable TLS for non-local environments |
| 5 | Message ordering | Preserved per HTTP connection (single TCP stream per client) | Not guaranteed across multiple consumers or when `prefetchCount > 1` | Low | Use a single consumer instance per queue if strict ordering is required; document relaxation |
| 6 | Service throttling | `serviceThrottling`: `maxConcurrentCalls=16`, `maxConcurrentSessions=100`, `maxConcurrentInstances=100` | QoS `prefetchCount` on the channel controls in-flight message count | Low | Call `channel.BasicQos(0, 16, false)` to mirror `maxConcurrentCalls=16` (already set to `1` in generated consumer; adjust as needed) |

---

## Section 4 — Verification Client Runbook

These steps produce two output files that should be identical except for the generated `OrderId` values.

### Prerequisites
- RabbitMQ broker accessible at `localhost:5672` with default credentials (`guest`/`guest`)
- .NET Framework 4.7.2 runtime installed
- Solution built in Release configuration (`msbuild /p:Configuration=Release`)

### Steps

**1. Start the WCF service**
```
cd samples\WcfOrderService\OrderService\bin\Release
OrderService.exe
```
Wait until the console prints: `Service started at http://localhost:8080/orders`

**2. Run the WCF Verification Client**
```
cd samples\OrchestratedMigration\generated\Verification\bin\Release
WcfVerificationClient.exe > wcf_output.txt
type wcf_output.txt
```
Expected console output (OrderId will vary):
```
[WCF] Starting WCF verification scenario...
[WCF] PlaceOrder | Success=True, OrderId=<GUID>, Total=$60.00
[WCF] GetOrderStatus | OrderId=<GUID>, Status=Pending
[WCF] CancelOrder | OrderId=<GUID> | sent (one-way)
```

**3. Ensure RabbitMQ broker is running**
```
rabbitmq-diagnostics ping
```
Broker must respond `Ping succeeded`. Default connection: `localhost:5672`, vhost `/`, credentials `guest`/`guest`.

**4. Start the RabbitMQ Consumer**
```
cd samples\OrchestratedMigration\generated\Consumer\bin\Release
Consumer.exe
```
Wait until the console prints: `[Consumer] Listening on queue 'order-service'...`

**5. Run the RabbitMQ Verification Client**
```
cd samples\OrchestratedMigration\generated\Verification\bin\Release
RabbitMqVerificationClient.exe > rabbitmq_output.txt
type rabbitmq_output.txt
```
Expected console output (OrderId will vary):
```
[RabbitMQ] Starting RabbitMQ verification scenario...
[RabbitMQ] PlaceOrder | Success=True, OrderId=<ID>, Total=$60.00
[RabbitMQ] GetOrderStatus | OrderId=<ID>, Status=Pending
[RabbitMQ] CancelOrder | OrderId=<ID> | sent (one-way)
```

**6. Compare outputs**
```
diff wcf_output.txt rabbitmq_output.txt
```

**Expected diff results:**

| Field | Must match exactly | Will differ |
|---|---|---|
| `Success` | ✅ `True` in both | — |
| `Total` | ✅ `$60.00` in both | — |
| `Status` | ✅ `Pending` in both | — |
| `OrderId` | — | ✅ Different per run (generated GUID vs short alphanumeric ID) |
| Output prefix | — | `[WCF]` vs `[RabbitMQ]` (expected) |

The only lines that should differ are those containing `OrderId` values and the `[WCF]`/`[RabbitMQ]` prefixes. Any other difference indicates a behavioral regression.

---

## Section 5 — Decommission Checklist

Complete these steps only after the RabbitMQ implementation has been running in production without incidents for a defined stabilization period (recommended: 2 weeks).

- [ ] All callers migrated to `OrderServiceRabbitMqClient` (search for `OrderServiceClient`, `ClientBase<IOrderService>`, `ChannelFactory<IOrderService>` across all repos)
- [ ] No `ClientBase<T>` / `ChannelFactory<T>` references to `IOrderService` remain in production code
- [ ] WCF endpoint traffic confirmed zero (monitor `http://localhost:8080/orders` request logs for 48 hours)
- [ ] `<system.serviceModel>` blocks removed from all application config files
- [ ] WCF assembly references removed from all project files (`System.ServiceModel`, `System.Runtime.Serialization`)
- [ ] WCF source files archived or deleted (requires team approval and PR review)
  - `OrderService.Contracts/IOrderService.cs`
  - `OrderService.Contracts/DataContracts.cs`
  - `OrderService/OrderServiceImpl.cs`
  - `OrderService/ValidationFault.cs`
  - `OrderService/Program.cs`
  - `OrderService.Client/OrderServiceClient.cs`
  - `OrderService.Client/Program.cs`
- [ ] RabbitMQ dead-letter queue (`order-service.dlq` or broker default) monitored and alert configured
- [ ] `OrderService.Tests` project decommissioned or replaced by `Tests/OrderServiceConsumerTests` in CI pipeline
