# RabbitMQ Order Service — Migration from WCF

This project is the RabbitMQ-based replacement for the WCF `OrderService` sample.
It was produced by the **wcf-to-rabbitmq-migration** agent following a 5-phase migration workflow.

---

## WCF → RabbitMQ Concept Mapping

| WCF concept | RabbitMQ equivalent |
|---|---|
| `[ServiceContract]` on `IOrderService` | Exchange `"order-service"` (direct, durable) |
| `[OperationContract] PlaceOrder` (request-reply) | Routing key `"place-order"` · messages: `PlaceOrderMessage` / `PlaceOrderResponse` |
| `[OperationContract] GetOrderStatus` (request-reply) | Routing key `"get-order-status"` · messages: `GetOrderStatusMessage` / `GetOrderStatusResponse` |
| `[OperationContract(IsOneWay=true)] CancelOrder` | Routing key `"cancel-order"` · message: `CancelOrderMessage` (fire-and-forget) |
| `[DataContract] OrderRequest` | `PlaceOrderMessage` (plain C# class, no WCF attributes) |
| `[DataContract] OrderLine` | `OrderLineDto` (nested in `PlaceOrderMessage`) |
| `[DataContract] OrderConfirmation` | `PlaceOrderResponse` |
| `[DataContract] OrderStatus` | `GetOrderStatusResponse` |
| `FaultException<ValidationFault>` | `ErrorReplyMessage` published to the client's reply queue |
| `ServiceHost` + `OrderServiceImpl` | `OrderServiceConsumer` (EventingBasicConsumer) |
| `OrderServiceClient` (ClientBase\<T\>) | `OrderServiceRabbitMqClient` (async RPC via exclusive reply queues) |
| `<system.serviceModel>` in App.config | `<appSettings>` with `RabbitMq:*` keys |

---

## Directory Structure

```
RabbitMqOrderService/
├── README.md
├── packages.txt                         NuGet packages required
├── Messages/
│   ├── PlaceOrderMessage.cs
│   ├── PlaceOrderResponse.cs
│   ├── GetOrderStatusMessage.cs
│   ├── GetOrderStatusResponse.cs
│   ├── CancelOrderMessage.cs
│   └── ErrorReplyMessage.cs
├── Infrastructure/
│   ├── AppConfig.cs                     Reads RabbitMq:* from appSettings
│   └── RabbitMqConnectionFactory.cs     Builds IConnectionFactory
├── Consumer/
│   ├── OrderServiceConsumer.cs          OrderLogic + EventingBasicConsumer host
│   └── Program.cs                       Console host entry point
├── Client/
│   ├── OrderServiceRabbitMqClient.cs    Async RPC client (replaces ClientBase<T>)
│   └── Program.cs                       Demo client
├── Config/
│   └── App.config                       RabbitMQ connection appSettings
└── Tests/
    └── OrderServiceConsumerTests.cs     MSTest unit tests (no broker needed)
```

---

## How to Run

### Prerequisites
- .NET Framework 4.7.2 (or .NET 6+ with the `net6.0` TFM)
- RabbitMQ 3.x or later running locally on the default port (5672)
- NuGet packages listed in `packages.txt` restored

### 1. Restore NuGet packages
```
nuget restore RabbitMqOrderService.sln
```

### 2. Build
```
msbuild RabbitMqOrderService.sln /p:Configuration=Release
```

### 3. Start the consumer (server)
```
Consumer\bin\Release\RabbitMqOrderService.Consumer.exe
```

### 4. Run the demo client (in a second terminal)
```
Client\bin\Release\RabbitMqOrderService.Client.exe
```

### 5. Run the unit tests (no broker needed)
```
vstest.console.exe Tests\bin\Release\RabbitMqOrderService.Tests.dll
```

---

## Migration Notes

### Sync → Async
WCF operations block the calling thread. The RabbitMQ client uses `async/await` and
`TaskCompletionSource<T>` so the thread is not blocked while waiting for the reply.

### At-least-once delivery
RabbitMQ with manual acknowledgement gives *at-least-once* semantics. The consumer must be
idempotent. The original WCF service was effectively *at-most-once* (HTTP). Introduce an
idempotency key (e.g., `CorrelationId`) and a deduplication store for production use.

### Fault model change
WCF `FaultException<T>` propagates an exception back to the caller. In the RabbitMQ model
validation errors are returned as `ErrorReplyMessage` on the reply queue. Clients should
inspect the response type (or a discriminator field) rather than catching exceptions.

### Order of replies
RabbitMQ does not guarantee order across parallel consumers. If you scale consumers to
multiple instances, use `CorrelationId` to match replies to requests (already implemented
via `ConcurrentDictionary<string, TaskCompletionSource<T>>`).

### Connection recovery
`AutomaticRecoveryEnabled = true` on the `ConnectionFactory` re-creates channels and
re-subscribes consumers after a broker restart. Inflight messages should be expected to be
re-delivered after a recovery event.

### Security
The example uses plaintext AMQP. For production, enable TLS (`AmqpTcpEndpoint` with SSL),
use a dedicated vhost, and grant only the required permissions to the service account.

---

## Verification

> **Note:** A live RabbitMQ instance was **not** available in this environment.
> The following commands describe how verification should be performed manually.

### Build command
```
msbuild RabbitMqOrderService.sln /p:Configuration=Release
```

### Test command
```
vstest.console.exe Tests\bin\Release\RabbitMqOrderService.Tests.dll
```

### Manual smoke-test steps (requires a running RabbitMQ broker)
1. Install RabbitMQ locally (or run `docker run -p 5672:5672 rabbitmq:3-management`).
2. Ensure `Config/App.config` points to the correct host/credentials.
3. Start `Consumer\bin\Release\RabbitMqOrderService.Consumer.exe`.
4. In the RabbitMQ Management UI (`http://localhost:15672`) confirm that exchange
   `"order-service"` and queue `"order-service"` were created.
5. Run `Client\bin\Release\RabbitMqOrderService.Client.exe`.
6. Confirm that the client prints a successful PlaceOrder response with an OrderId,
   a GetOrderStatus response showing "Pending", and a CancelOrder confirmation.
7. Rerun GetOrderStatus after Cancel and confirm the status is now "Cancelled".
