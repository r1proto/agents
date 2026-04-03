# WCF Order Service — Example Application

This is a minimal but realistic .NET Framework 4.7.2 WCF application used as the migration source for the `wcf-to-rabbitmq-migration` agent demo.

## Solution Structure

```
WcfOrderService/
├── WcfOrderService.sln
├── OrderService.Contracts/       # Shared [ServiceContract] and [DataContract] types
│   ├── IOrderService.cs          # [ServiceContract] with 3 operations
│   └── DataContracts.cs          # OrderRequest, OrderLine, OrderConfirmation, OrderStatus
├── OrderService/                 # Self-hosted WCF service
│   ├── OrderServiceImpl.cs       # [ServiceContract] implementation
│   ├── ValidationFault.cs        # [DataContract] fault detail
│   ├── Program.cs                # ServiceHost entry point
│   └── App.config                # <system.serviceModel> server config
├── OrderService.Client/          # WCF client console app
│   ├── OrderServiceClient.cs     # ClientBase<IOrderService> proxy
│   ├── Program.cs                # Demo client usage
│   └── App.config                # <system.serviceModel> client config
└── OrderService.Tests/           # MSTest unit tests for the service impl
    └── OrderServiceTests.cs
```

## WCF Patterns Demonstrated

| Pattern | Where |
|---|---|
| `[ServiceContract]` | `IOrderService.cs` |
| `[OperationContract]` (request-reply) | `PlaceOrder`, `GetOrderStatus` |
| `[OperationContract(IsOneWay = true)]` | `CancelOrder` |
| `[DataContract]` / `[DataMember]` | `DataContracts.cs` |
| `FaultException<T>` | `OrderServiceImpl.cs` |
| `ServiceHost` self-hosting | `OrderService/Program.cs` |
| `ClientBase<T>` proxy | `OrderServiceClient.cs` |
| `<system.serviceModel>` config | `App.config` files |
| `basicHttpBinding` | Both `App.config` files |
| `serviceThrottling` behavior | Server `App.config` |

## Running Locally

1. Start the service: run `OrderService.exe` (requires .NET Framework 4.7.2 and local HTTP listener permission).
2. Run the client: run `OrderService.Client.exe`.
3. Run tests: open in Visual Studio and run with MSTest, or use `vstest.console.exe`.
