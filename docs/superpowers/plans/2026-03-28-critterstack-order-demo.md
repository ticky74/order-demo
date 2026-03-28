# Critterstack Order Demo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full Critterstack (Marten + WolverineFX) POC — an ice hockey goalie equipment store with Order, Inventory, and Payment microservices, a relational read model, and a React frontend.

**Architecture:** Each service owns its own PostgreSQL event store (Marten) and communicates via RabbitMQ using Wolverine's transactional outbox pattern. A dedicated Projector service subscribes to all domain events and maintains a relational read model in a separate database. All infrastructure is orchestrated by .NET Aspire.

**Tech Stack:** .NET 10, .NET Aspire 13.x, Marten 7.x, WolverineFx 3.x, WolverineFx.Marten, WolverineFx.RabbitMQ, EF Core + Npgsql, React 18 + TypeScript + Vite + shadcn/ui + TanStack Query

---

## File Map

```
src/
  AppHost/
    AppHost.csproj
    Program.cs

  ServiceDefaults/
    ServiceDefaults.csproj
    Extensions.cs

  Contracts/
    Contracts.csproj
    Events/OrderEvents.cs
    Events/InventoryEvents.cs
    Events/PaymentEvents.cs
    Models/OrderItem.cs

  Inventory/Inventory.Api/
    Inventory.Api.csproj
    Program.cs
    Domain/InventoryItem.cs
    Handlers/Events/OrderPlacedHandler.cs
    Handlers/Events/PaymentFailedHandler.cs
    Infrastructure/InventorySeeder.cs
    Endpoints/InventoryEndpoints.cs

  Order/Order.Api/
    Order.Api.csproj
    Program.cs
    Domain/Order.cs
    Commands/PlaceOrderCommand.cs
    Handlers/Commands/PlaceOrderHandler.cs
    Handlers/Events/StockReservationFailedHandler.cs
    Handlers/Events/PaymentProcessedHandler.cs
    Handlers/Events/PaymentFailedHandler.cs
    Endpoints/OrderEndpoints.cs

  Payment/Payment.Api/
    Payment.Api.csproj
    Program.cs
    Domain/Payment.cs
    Handlers/Events/StockReservedHandler.cs
    Endpoints/PaymentEndpoints.cs

  ReadModel/ReadModel.Projector/
    ReadModel.Projector.csproj
    Program.cs
    Data/AppDbContext.cs
    Data/Migrations/  (EF Core auto-generated)
    Models/OrderReadModel.cs
    Models/OrderLineReadModel.cs
    Models/InventoryItemReadModel.cs
    Handlers/OrderProjection.cs
    Handlers/InventoryProjection.cs
    Endpoints/CatalogEndpoints.cs

  Frontend/frontend/
    package.json
    vite.config.ts
    tsconfig.json
    index.html
    src/
      main.tsx
      App.tsx
      lib/api.ts
      lib/users.ts
      types/index.ts
      hooks/useCart.ts
      components/NavBar.tsx
      components/UserSwitcher.tsx
      components/ProductCard.tsx
      components/CartSidebar.tsx
      components/OrderStatusBadge.tsx
      pages/ShopPage.tsx
      pages/CheckoutPage.tsx
      pages/OrdersPage.tsx
      pages/OrderDetailPage.tsx

tests/
  Inventory.Api.Tests/
    Inventory.Api.Tests.csproj
    Domain/InventoryItemTests.cs
    Handlers/OrderPlacedHandlerTests.cs

  Order.Api.Tests/
    Order.Api.Tests.csproj
    Domain/OrderTests.cs
    Handlers/PlaceOrderHandlerTests.cs

  Payment.Api.Tests/
    Payment.Api.Tests.csproj
    Domain/PaymentTests.cs
```

---

## Task 1: Solution Scaffolding

**Files:**
- Create: `src/AppHost/AppHost.csproj`
- Create: `src/ServiceDefaults/ServiceDefaults.csproj`
- Create: `src/Contracts/Contracts.csproj`
- Modify: `OrderDemo.slnx`

- [ ] **Step 1: Create directory structure**

```bash
cd /path/to/order-demo
mkdir -p src/AppHost src/ServiceDefaults src/Contracts
mkdir -p src/Inventory/Inventory.Api
mkdir -p src/Order/Order.Api
mkdir -p src/Payment/Payment.Api
mkdir -p src/ReadModel/ReadModel.Projector
mkdir -p src/Frontend
mkdir -p tests/Inventory.Api.Tests tests/Order.Api.Tests tests/Payment.Api.Tests
```

- [ ] **Step 2: Scaffold AppHost**

```bash
dotnet new aspire-apphost -n AppHost -o src/AppHost
```

- [ ] **Step 3: Scaffold ServiceDefaults**

```bash
dotnet new aspire-servicedefaults -n ServiceDefaults -o src/ServiceDefaults
```

- [ ] **Step 4: Scaffold Contracts class library**

```bash
dotnet new classlib -n Contracts -f net10.0 -o src/Contracts
rm src/Contracts/Class1.cs
```

- [ ] **Step 5: Scaffold API projects**

```bash
dotnet new webapi -n Inventory.Api -f net10.0 -o src/Inventory/Inventory.Api --no-openapi
dotnet new webapi -n Order.Api -f net10.0 -o src/Order/Order.Api --no-openapi
dotnet new webapi -n Payment.Api -f net10.0 -o src/Payment/Payment.Api --no-openapi
dotnet new webapi -n ReadModel.Projector -f net10.0 -o src/ReadModel/ReadModel.Projector --no-openapi
```

- [ ] **Step 6: Scaffold test projects**

```bash
dotnet new xunit -n Inventory.Api.Tests -f net10.0 -o tests/Inventory.Api.Tests
dotnet new xunit -n Order.Api.Tests -f net10.0 -o tests/Order.Api.Tests
dotnet new xunit -n Payment.Api.Tests -f net10.0 -o tests/Payment.Api.Tests
```

- [ ] **Step 7: Add all projects to solution**

```bash
dotnet sln OrderDemo.slnx add src/AppHost/AppHost.csproj
dotnet sln OrderDemo.slnx add src/ServiceDefaults/ServiceDefaults.csproj
dotnet sln OrderDemo.slnx add src/Contracts/Contracts.csproj
dotnet sln OrderDemo.slnx add src/Inventory/Inventory.Api/Inventory.Api.csproj
dotnet sln OrderDemo.slnx add src/Order/Order.Api/Order.Api.csproj
dotnet sln OrderDemo.slnx add src/Payment/Payment.Api/Payment.Api.csproj
dotnet sln OrderDemo.slnx add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj
dotnet sln OrderDemo.slnx add tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj
dotnet sln OrderDemo.slnx add tests/Order.Api.Tests/Order.Api.Tests.csproj
dotnet sln OrderDemo.slnx add tests/Payment.Api.Tests/Payment.Api.Tests.csproj
```

- [ ] **Step 8: Add project references**

```bash
# All services reference ServiceDefaults and Contracts
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj reference src/ServiceDefaults/ServiceDefaults.csproj src/Contracts/Contracts.csproj
dotnet add src/Order/Order.Api/Order.Api.csproj reference src/ServiceDefaults/ServiceDefaults.csproj src/Contracts/Contracts.csproj
dotnet add src/Payment/Payment.Api/Payment.Api.csproj reference src/ServiceDefaults/ServiceDefaults.csproj src/Contracts/Contracts.csproj
dotnet add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj reference src/ServiceDefaults/ServiceDefaults.csproj src/Contracts/Contracts.csproj

# AppHost references all service projects
dotnet add src/AppHost/AppHost.csproj reference src/Inventory/Inventory.Api/Inventory.Api.csproj
dotnet add src/AppHost/AppHost.csproj reference src/Order/Order.Api/Order.Api.csproj
dotnet add src/AppHost/AppHost.csproj reference src/Payment/Payment.Api/Payment.Api.csproj
dotnet add src/AppHost/AppHost.csproj reference src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj

# Test projects reference their service
dotnet add tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj reference src/Inventory/Inventory.Api/Inventory.Api.csproj src/Contracts/Contracts.csproj
dotnet add tests/Order.Api.Tests/Order.Api.Tests.csproj reference src/Order/Order.Api/Order.Api.csproj src/Contracts/Contracts.csproj
dotnet add tests/Payment.Api.Tests/Payment.Api.Tests.csproj reference src/Payment/Payment.Api/Payment.Api.csproj src/Contracts/Contracts.csproj
```

- [ ] **Step 9: Commit**

```bash
git init
git add .
git commit -m "chore: scaffold solution structure"
```

---

## Task 2: Contracts Project

**Files:**
- Create: `src/Contracts/Models/OrderItem.cs`
- Create: `src/Contracts/Events/OrderEvents.cs`
- Create: `src/Contracts/Events/InventoryEvents.cs`
- Create: `src/Contracts/Events/PaymentEvents.cs`

- [ ] **Step 1: Create Models/OrderItem.cs**

```csharp
// src/Contracts/Models/OrderItem.cs
namespace Contracts.Models;

public record OrderItem(
    Guid ItemId,
    string ItemName,
    string Brand,
    int Quantity,
    decimal UnitPrice);
```

- [ ] **Step 2: Create Events/OrderEvents.cs**

```csharp
// src/Contracts/Events/OrderEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record OrderPlaced(
    Guid OrderId,
    Guid UserId,
    OrderItem[] Items,
    decimal Total);

public record OrderConfirmed(
    Guid OrderId,
    DateTimeOffset ConfirmedAt);

public record OrderFailed(
    Guid OrderId,
    string Reason);
```

- [ ] **Step 3: Create Events/InventoryEvents.cs**

```csharp
// src/Contracts/Events/InventoryEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record InventoryItemCreated(
    Guid ItemId,
    string Brand,
    string Category,
    string Name,
    string Description,
    decimal Price,
    int InitialStock);

// StockReserved includes Items so compensating handlers know quantities
public record StockReserved(
    Guid OrderId,
    OrderItem[] Items);

public record StockReservationFailed(
    Guid OrderId,
    string Reason);

// Includes Items so Inventory knows how much stock to restore
public record StockReleased(
    Guid OrderId,
    OrderItem[] Items);

public record StockQuantityUpdated(
    Guid ItemId,
    int NewQuantity);
```

- [ ] **Step 4: Create Events/PaymentEvents.cs**

```csharp
// src/Contracts/Events/PaymentEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record PaymentProcessed(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount);

// Includes Items so Inventory can issue compensating StockReleased
public record PaymentFailed(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    OrderItem[] Items);
```

- [ ] **Step 5: Verify Contracts builds with zero dependencies**

```bash
dotnet build src/Contracts/Contracts.csproj
```

Expected: Build succeeded. No warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Contracts/
git commit -m "feat: add Contracts project with domain events and types"
```

---

## Task 3: AppHost — Aspire Infrastructure Wiring

**Files:**
- Modify: `src/AppHost/Program.cs`
- Modify: `src/AppHost/AppHost.csproj`

- [ ] **Step 1: Add Aspire hosting packages to AppHost**

```bash
dotnet add src/AppHost/AppHost.csproj package Aspire.Hosting.PostgreSQL
dotnet add src/AppHost/AppHost.csproj package Aspire.Hosting.RabbitMQ
dotnet add src/AppHost/AppHost.csproj package Aspire.Hosting.JavaScript
```

- [ ] **Step 2: Write src/AppHost/Program.cs**

```csharp
// src/AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// ── PostgreSQL ─────────────────────────────────────────────
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("orderdemo-postgres-data")
    .WithPgAdmin(pgAdminPort: 5050);

var ordersDb    = postgres.AddDatabase("orders-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentsDb  = postgres.AddDatabase("payments-db");
var readModelDb = postgres.AddDatabase("readmodel-db");

// ── RabbitMQ ───────────────────────────────────────────────
var rabbit = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithDataVolume("orderdemo-rabbitmq-data")
    .WithManagementPlugin(port: 15672);

// ── Services ───────────────────────────────────────────────
var inventoryApi = builder.AddProject<Projects.Inventory_Api>("inventory-api")
    .WithHttpsEndpoint(port: 7001)
    .WithReference(inventoryDb).WaitFor(inventoryDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

var orderApi = builder.AddProject<Projects.Order_Api>("order-api")
    .WithHttpsEndpoint(port: 7002)
    .WithReference(ordersDb).WaitFor(ordersDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

var paymentApi = builder.AddProject<Projects.Payment_Api>("payment-api")
    .WithHttpsEndpoint(port: 7003)
    .WithReference(paymentsDb).WaitFor(paymentsDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

var projector = builder.AddProject<Projects.ReadModel_Projector>("readmodel-projector")
    .WithHttpsEndpoint(port: 7004)
    .WithReference(readModelDb).WaitFor(readModelDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithExternalHttpEndpoints();

// ── Frontend ───────────────────────────────────────────────
builder.AddViteApp("frontend", "../Frontend/frontend")
    .WithNpmPackageInstallation()
    .WithHttpEndpoint(port: 3000, env: "VITE_PORT")
    .WithReference(inventoryApi)
    .WithReference(orderApi)
    .WithReference(projector)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

- [ ] **Step 3: Verify AppHost builds**

```bash
dotnet build src/AppHost/AppHost.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/AppHost/
git commit -m "feat: configure Aspire AppHost with Postgres, RabbitMQ, and all services"
```

---

## Task 4: Inventory.Api — Project Setup

**Files:**
- Modify: `src/Inventory/Inventory.Api/Inventory.Api.csproj`
- Create: `src/Inventory/Inventory.Api/Program.cs`

- [ ] **Step 1: Add NuGet packages**

```bash
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj package Marten
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj package WolverineFx
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj package WolverineFx.Marten
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj package WolverineFx.RabbitMQ
dotnet add src/Inventory/Inventory.Api/Inventory.Api.csproj package Aspire.Npgsql
```

- [ ] **Step 2: Write src/Inventory/Inventory.Api/Program.cs**

```csharp
// src/Inventory/Inventory.Api/Program.cs
using Contracts.Events;
using Inventory.Api.Endpoints;
using Inventory.Api.Infrastructure;
using Marten;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── Marten (event store) ────────────────────────────────────
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("inventory-db")!);
    opts.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

// ── Wolverine (messaging) ────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .AutoPurgeOnStartup();   // clean queues on dev restart

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Publish inventory events to the inventory-events exchange
    opts.PublishMessage<InventoryItemCreated>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReserved>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReservationFailed>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReleased>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockQuantityUpdated>().ToRabbitExchange("inventory-events");

    // Listen to order-events and payment-events
    opts.ListenToRabbitQueue("inventory-api-inbox")
        .BindExchange("order-events")
        .BindExchange("payment-events");
});

builder.Services.AddHostedService<InventorySeeder>();

// ── CORS (allow frontend dev server) ────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseDefaultExceptionHandler();
app.UseCors();
app.MapDefaultEndpoints();
app.MapInventoryEndpoints();
app.Run();
```

- [ ] **Step 3: Verify it builds**

```bash
dotnet build src/Inventory/Inventory.Api/Inventory.Api.csproj
```

Expected: Build succeeded (may warn about missing handler/endpoint classes — that's fine, they come in later tasks).

- [ ] **Step 4: Commit**

```bash
git add src/Inventory/Inventory.Api/
git commit -m "feat: scaffold Inventory.Api with Marten and Wolverine wiring"
```

---

## Task 5: Inventory.Api — Domain Aggregate & Tests

**Files:**
- Create: `src/Inventory/Inventory.Api/Domain/InventoryItem.cs`
- Create: `tests/Inventory.Api.Tests/Domain/InventoryItemTests.cs`

- [ ] **Step 1: Write the failing tests first**

```csharp
// tests/Inventory.Api.Tests/Domain/InventoryItemTests.cs
using Contracts.Events;
using Inventory.Api.Domain;

namespace Inventory.Api.Tests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void Apply_InventoryItemCreated_SetsAllProperties()
    {
        var item = new InventoryItem();
        var itemId = Guid.NewGuid();
        item.Apply(new InventoryItemCreated(
            itemId, "Bauer", "Mask", "Bauer Hyperlite2", "Top mask", 899.99m, 8));

        Assert.Equal(itemId, item.Id);
        Assert.Equal("Bauer", item.Brand);
        Assert.Equal("Mask", item.Category);
        Assert.Equal("Bauer Hyperlite2", item.Name);
        Assert.Equal(899.99m, item.Price);
        Assert.Equal(8, item.StockQuantity);
    }

    [Fact]
    public void Apply_StockQuantityUpdated_UpdatesStock()
    {
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Mask", "Hyperlite2", "", 899.99m, 8));
        item.Apply(new StockQuantityUpdated(item.Id, 5));

        Assert.Equal(5, item.StockQuantity);
    }

    [Fact]
    public void HasSufficientStock_ReturnsTrueWhenEnough()
    {
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Mask", "Hyperlite2", "", 899.99m, 8));

        Assert.True(item.HasSufficientStock(8));
        Assert.False(item.HasSufficientStock(9));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj --filter "FullyQualifiedName~InventoryItemTests"
```

Expected: FAIL — `Inventory.Api.Domain.InventoryItem` not found.

- [ ] **Step 3: Implement src/Inventory/Inventory.Api/Domain/InventoryItem.cs**

```csharp
// src/Inventory/Inventory.Api/Domain/InventoryItem.cs
using Contracts.Events;

namespace Inventory.Api.Domain;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public string Brand { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public void Apply(InventoryItemCreated evt)
    {
        Id = evt.ItemId;
        Brand = evt.Brand;
        Category = evt.Category;
        Name = evt.Name;
        Description = evt.Description;
        Price = evt.Price;
        StockQuantity = evt.InitialStock;
    }

    public void Apply(StockQuantityUpdated evt) =>
        StockQuantity = evt.NewQuantity;

    public bool HasSufficientStock(int required) =>
        StockQuantity >= required;
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj --filter "FullyQualifiedName~InventoryItemTests"
```

Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Inventory/Inventory.Api/Domain/ tests/Inventory.Api.Tests/Domain/
git commit -m "feat: add InventoryItem aggregate with event sourcing"
```

---

## Task 6: Inventory.Api — Seeding

**Files:**
- Create: `src/Inventory/Inventory.Api/Infrastructure/InventorySeeder.cs`

- [ ] **Step 1: Write src/Inventory/Inventory.Api/Infrastructure/InventorySeeder.cs**

```csharp
// src/Inventory/Inventory.Api/Infrastructure/InventorySeeder.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Inventory.Api.Infrastructure;

public class InventorySeeder(IDocumentStore store, IMessageBus bus) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var session = store.LightweightSession();

        // Check if already seeded — look for any InventoryItemCreated events
        var existing = await session.Events
            .QueryRawEventDataOnly<InventoryItemCreated>()
            .AnyAsync(cancellationToken);

        if (existing) return;

        var products = new[]
        {
            new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Mask",
                "Bauer Hyperlite2 Goalie Mask",
                "Professional-grade fibreglass composite mask with Pro-Cert certified cage.", 899.99m, 8),
            new InventoryItemCreated(Guid.NewGuid(), "CCM", "Mask",
                "CCM Axis 2.9 Goalie Mask",
                "Lightweight VN foam liner with full-wrap protection and titanium cage.", 749.99m, 10),
            new InventoryItemCreated(Guid.NewGuid(), "True", "Mask",
                "True Catalyst 9X2 Goalie Mask",
                "Carbon fibre shell with custom-fit padding system.", 849.99m, 6),

            new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Pads",
                "Bauer Hyperlite2 Leg Pads",
                "Ultra-lightweight 3-piece core with max rebound angle control.", 1299.99m, 5),
            new InventoryItemCreated(Guid.NewGuid(), "CCM", "Pads",
                "CCM Axis A2.9 Leg Pads",
                "Nash outer with D30 Smart foam knee stack.", 1099.99m, 7),
            new InventoryItemCreated(Guid.NewGuid(), "True", "Pads",
                "True Catalyst 9X2 Leg Pads",
                "Form-fit boot with True's Calf Wrap System.", 1249.99m, 4),

            new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Glove & Blocker",
                "Bauer Hyperlite2 Catch Glove",
                "Max-open pocket with moisture-wicking liner.", 449.99m, 12),
            new InventoryItemCreated(Guid.NewGuid(), "CCM", "Glove & Blocker",
                "CCM Axis 2.9 Blocker",
                "High-density foam board with Nash palm insert.", 349.99m, 15),
            new InventoryItemCreated(Guid.NewGuid(), "True", "Glove & Blocker",
                "True Catalyst 9X2 Catch Glove",
                "T-Flex closure system with adjustable break angle.", 399.99m, 9),

            new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Chest & Arms",
                "Bauer Supreme Mach C/A Protector",
                "Floating sternum design with D30 shoulder caps.", 599.99m, 6),
            new InventoryItemCreated(Guid.NewGuid(), "CCM", "Chest & Arms",
                "CCM Axis A2.9 C/A Protector",
                "Dual-density foam arms with articulated shoulder floaters.", 549.99m, 8),
            new InventoryItemCreated(Guid.NewGuid(), "True", "Chest & Arms",
                "True Catalyst 9X2 C/A Protector",
                "Form-fitting chassis with True's Dynamic Fit System.", 579.99m, 5),
        };

        foreach (var product in products)
        {
            session.Events.StartStream<Inventory.Api.Domain.InventoryItem>(product.ItemId, product);
            await bus.PublishAsync(product);   // flows to read model via RabbitMQ outbox
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/Inventory/Inventory.Api/Inventory.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Inventory/Inventory.Api/Infrastructure/
git commit -m "feat: add inventory seeder with 12 ice hockey goalie products"
```

---

## Task 7: Inventory.Api — Event Handlers

**Files:**
- Create: `src/Inventory/Inventory.Api/Handlers/Events/OrderPlacedHandler.cs`
- Create: `src/Inventory/Inventory.Api/Handlers/Events/PaymentFailedHandler.cs`
- Create: `tests/Inventory.Api.Tests/Handlers/OrderPlacedHandlerTests.cs`

- [ ] **Step 1: Write failing handler tests**

```csharp
// tests/Inventory.Api.Tests/Handlers/OrderPlacedHandlerTests.cs
using Contracts.Events;
using Contracts.Models;
using Inventory.Api.Domain;

namespace Inventory.Api.Tests.Handlers;

public class OrderPlacedHandlerTests
{
    [Fact]
    public void ReserveStock_ShouldReturnStockReserved_WhenSufficientStock()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(itemId, "Bauer", "Mask", "Hyperlite2", "", 899.99m, 5));

        var orderItems = new[] { new OrderItem(itemId, "Hyperlite2", "Bauer", 2, 899.99m) };
        var orderId = Guid.NewGuid();

        // Act
        var (success, stockReserved, stockFailed, updatedQty) =
            OrderPlacedHandlerLogic.TryReserve(orderId, orderItems, new[] { item });

        // Assert
        Assert.True(success);
        Assert.NotNull(stockReserved);
        Assert.Null(stockFailed);
        Assert.Equal(3, updatedQty[itemId]);  // 5 - 2
    }

    [Fact]
    public void ReserveStock_ShouldReturnFailed_WhenInsufficientStock()
    {
        var itemId = Guid.NewGuid();
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(itemId, "Bauer", "Mask", "Hyperlite2", "", 899.99m, 1));

        var orderItems = new[] { new OrderItem(itemId, "Hyperlite2", "Bauer", 3, 899.99m) };

        var (success, stockReserved, stockFailed, _) =
            OrderPlacedHandlerLogic.TryReserve(Guid.NewGuid(), orderItems, new[] { item });

        Assert.False(success);
        Assert.Null(stockReserved);
        Assert.NotNull(stockFailed);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj --filter "FullyQualifiedName~OrderPlacedHandlerTests"
```

Expected: FAIL — `OrderPlacedHandlerLogic` not found.

- [ ] **Step 3: Implement OrderPlacedHandler**

```csharp
// src/Inventory/Inventory.Api/Handlers/Events/OrderPlacedHandler.cs
using Contracts.Events;
using Contracts.Models;
using Inventory.Api.Domain;
using Marten;
using Wolverine;

namespace Inventory.Api.Handlers.Events;

// Pure logic extracted for testability
public static class OrderPlacedHandlerLogic
{
    public static (bool success, StockReserved? reserved, StockReservationFailed? failed,
        Dictionary<Guid, int> updatedQuantities)
        TryReserve(Guid orderId, OrderItem[] orderItems, IEnumerable<InventoryItem> items)
    {
        var itemLookup = items.ToDictionary(i => i.Id);
        var updatedQuantities = new Dictionary<Guid, int>();

        foreach (var orderItem in orderItems)
        {
            if (!itemLookup.TryGetValue(orderItem.ItemId, out var item) ||
                !item.HasSufficientStock(orderItem.Quantity))
            {
                var reason = $"Insufficient stock for '{orderItem.ItemName}' " +
                             $"(requested: {orderItem.Quantity}, available: {item?.StockQuantity ?? 0})";
                return (false, null, new StockReservationFailed(orderId, reason), updatedQuantities);
            }
            updatedQuantities[orderItem.ItemId] = item.StockQuantity - orderItem.Quantity;
        }

        return (true, new StockReserved(orderId, orderItems), null, updatedQuantities);
    }
}

public static class OrderPlacedHandler
{
    public static async Task Handle(
        OrderPlaced evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Load all inventory items involved in the order
        var itemIds = evt.Items.Select(i => i.ItemId).ToArray();
        var items = new List<InventoryItem>();

        foreach (var itemId in itemIds)
        {
            var item = await session.Events.AggregateStreamAsync<InventoryItem>(itemId);
            if (item is not null) items.Add(item);
        }

        var (success, reserved, failed, updatedQtys) =
            OrderPlacedHandlerLogic.TryReserve(evt.OrderId, evt.Items, items);

        if (!success)
        {
            await bus.PublishAsync(failed!);
            return;
        }

        // Append StockQuantityUpdated events to each item's stream
        foreach (var (itemId, newQty) in updatedQtys)
        {
            session.Events.Append(itemId, new StockQuantityUpdated(itemId, newQty));
        }

        await bus.PublishAsync(reserved!);
        // Wolverine's AutoApplyTransactions calls SaveChangesAsync after this handler
    }
}
```

- [ ] **Step 4: Implement PaymentFailedHandler**

```csharp
// src/Inventory/Inventory.Api/Handlers/Events/PaymentFailedHandler.cs
using Contracts.Events;
using Inventory.Api.Domain;
using Marten;
using Wolverine;

namespace Inventory.Api.Handlers.Events;

public static class PaymentFailedHandler
{
    public static async Task Handle(
        PaymentFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Restore stock for each item in the failed order
        foreach (var orderItem in evt.Items)
        {
            var item = await session.Events.AggregateStreamAsync<InventoryItem>(orderItem.ItemId);
            if (item is null) continue;

            var restoredQty = item.StockQuantity + orderItem.Quantity;
            session.Events.Append(orderItem.ItemId, new StockQuantityUpdated(orderItem.ItemId, restoredQty));
        }

        await bus.PublishAsync(new StockReleased(evt.OrderId, evt.Items));
        // Wolverine's AutoApplyTransactions calls SaveChangesAsync after this handler
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj --filter "FullyQualifiedName~OrderPlacedHandlerTests"
```

Expected: PASS — 2 tests.

- [ ] **Step 6: Verify build**

```bash
dotnet build src/Inventory/Inventory.Api/Inventory.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add src/Inventory/Inventory.Api/Handlers/ tests/Inventory.Api.Tests/Handlers/
git commit -m "feat: add Inventory event handlers for OrderPlaced and PaymentFailed"
```

---

## Task 8: Inventory.Api — HTTP Endpoints

**Files:**
- Create: `src/Inventory/Inventory.Api/Endpoints/InventoryEndpoints.cs`

- [ ] **Step 1: Write src/Inventory/Inventory.Api/Endpoints/InventoryEndpoints.cs**

```csharp
// src/Inventory/Inventory.Api/Endpoints/InventoryEndpoints.cs
namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Health check — Aspire also adds /health via MapDefaultEndpoints
        app.MapGet("/", () => Results.Ok(new { service = "inventory-api", status = "running" }));
        return app;
    }
}
```

> Note: Item browsing is served by the ReadModel.Projector (`/api/catalog/items`). Inventory.Api only handles commands and events; it does not expose item query endpoints.

- [ ] **Step 2: Verify the full Inventory.Api builds cleanly**

```bash
dotnet build src/Inventory/Inventory.Api/Inventory.Api.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Inventory/Inventory.Api/Endpoints/
git commit -m "feat: add Inventory.Api HTTP endpoints"
```

---

## Task 9: Order.Api — Project Setup

**Files:**
- Modify: `src/Order/Order.Api/Order.Api.csproj`
- Create: `src/Order/Order.Api/Program.cs`
- Create: `src/Order/Order.Api/Commands/PlaceOrderCommand.cs`

- [ ] **Step 1: Add NuGet packages**

```bash
dotnet add src/Order/Order.Api/Order.Api.csproj package Marten
dotnet add src/Order/Order.Api/Order.Api.csproj package WolverineFx
dotnet add src/Order/Order.Api/Order.Api.csproj package WolverineFx.Marten
dotnet add src/Order/Order.Api/Order.Api.csproj package WolverineFx.RabbitMQ
dotnet add src/Order/Order.Api/Order.Api.csproj package Aspire.Npgsql
```

- [ ] **Step 2: Create src/Order/Order.Api/Commands/PlaceOrderCommand.cs**

```csharp
// src/Order/Order.Api/Commands/PlaceOrderCommand.cs
using Contracts.Models;

namespace Order.Api.Commands;

public record PlaceOrderCommand(Guid OrderId, Guid UserId, OrderItem[] Items, decimal Total);
public record PlaceOrderResult(Guid OrderId);

public record PlaceOrderRequest(Guid UserId, OrderItem[] Items);
```

- [ ] **Step 3: Write src/Order/Order.Api/Program.cs**

```csharp
// src/Order/Order.Api/Program.cs
using Contracts.Events;
using Order.Api.Commands;
using Order.Api.Endpoints;
using Marten;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── Marten ──────────────────────────────────────────────────
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("orders-db")!);
    opts.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

// ── Wolverine ────────────────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .AutoPurgeOnStartup();

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Publish order events to the order-events exchange
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderConfirmed>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderFailed>().ToRabbitExchange("order-events");

    // Listen to inventory-events and payment-events
    opts.ListenToRabbitQueue("order-api-inbox")
        .BindExchange("inventory-events")
        .BindExchange("payment-events");
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseDefaultExceptionHandler();
app.UseCors();
app.MapDefaultEndpoints();
app.MapOrderEndpoints();
app.Run();
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/Order/Order.Api/Order.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/Order/Order.Api/
git commit -m "feat: scaffold Order.Api with Marten and Wolverine wiring"
```

---

## Task 10: Order.Api — Domain Aggregate & Tests

**Files:**
- Create: `src/Order/Order.Api/Domain/Order.cs`
- Create: `tests/Order.Api.Tests/Domain/OrderTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/Order.Api.Tests/Domain/OrderTests.cs
using Contracts.Events;
using Contracts.Models;

namespace Order.Api.Tests.Domain;

public class OrderTests
{
    private static OrderPlaced MakeOrderPlaced(Guid? orderId = null) =>
        new(orderId ?? Guid.NewGuid(), Guid.NewGuid(),
            [new OrderItem(Guid.NewGuid(), "Bauer Hyperlite2", "Bauer", 1, 899.99m)],
            899.99m);

    [Fact]
    public void Apply_OrderPlaced_SetsPendingStatus()
    {
        var order = new Order.Api.Domain.Order();
        order.Apply(MakeOrderPlaced());
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Apply_OrderConfirmed_SetsConfirmedStatus()
    {
        var order = new Order.Api.Domain.Order();
        var placed = MakeOrderPlaced();
        order.Apply(placed);
        order.Apply(new OrderConfirmed(placed.OrderId, DateTimeOffset.UtcNow));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Apply_OrderFailed_SetsFailedStatus()
    {
        var order = new Order.Api.Domain.Order();
        var placed = MakeOrderPlaced();
        order.Apply(placed);
        order.Apply(new OrderFailed(placed.OrderId, "Out of stock"));
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal("Out of stock", order.FailureReason);
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```bash
dotnet test tests/Order.Api.Tests/Order.Api.Tests.csproj --filter "FullyQualifiedName~OrderTests"
```

Expected: FAIL.

- [ ] **Step 3: Implement src/Order/Order.Api/Domain/Order.cs**

```csharp
// src/Order/Order.Api/Domain/Order.cs
using Contracts.Events;
using Contracts.Models;

namespace Order.Api.Domain;

public enum OrderStatus { Pending, Confirmed, Failed }

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OrderItem[] Items { get; private set; } = [];
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void Apply(OrderPlaced evt)
    {
        Id = evt.OrderId;
        UserId = evt.UserId;
        Items = evt.Items;
        TotalAmount = evt.Total;
        Status = OrderStatus.Pending;
        PlacedAt = DateTimeOffset.UtcNow;
    }

    public void Apply(OrderConfirmed evt)
    {
        Status = OrderStatus.Confirmed;
        ConfirmedAt = evt.ConfirmedAt;
    }

    public void Apply(OrderFailed evt)
    {
        Status = OrderStatus.Failed;
        FailureReason = evt.Reason;
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

```bash
dotnet test tests/Order.Api.Tests/Order.Api.Tests.csproj --filter "FullyQualifiedName~OrderTests"
```

Expected: PASS — 3 tests.

- [ ] **Step 5: Commit**

```bash
git add src/Order/Order.Api/Domain/ tests/Order.Api.Tests/Domain/
git commit -m "feat: add Order aggregate with event sourcing"
```

---

## Task 11: Order.Api — Command Handler & Event Handlers

**Files:**
- Create: `src/Order/Order.Api/Handlers/Commands/PlaceOrderHandler.cs`
- Create: `src/Order/Order.Api/Handlers/Events/StockReservationFailedHandler.cs`
- Create: `src/Order/Order.Api/Handlers/Events/PaymentProcessedHandler.cs`
- Create: `src/Order/Order.Api/Handlers/Events/PaymentFailedHandler.cs`

- [ ] **Step 1: Write src/Order/Order.Api/Handlers/Commands/PlaceOrderHandler.cs**

```csharp
// src/Order/Order.Api/Handlers/Commands/PlaceOrderHandler.cs
using Contracts.Events;
using Marten;
using Order.Api.Commands;
using Order.Api.Domain;
using Wolverine;

namespace Order.Api.Handlers.Commands;

public static class PlaceOrderHandler
{
    public static async Task<PlaceOrderResult> Handle(
        PlaceOrderCommand cmd,
        IDocumentSession session,
        IMessageBus bus)
    {
        var evt = new OrderPlaced(cmd.OrderId, cmd.UserId, cmd.Items, cmd.Total);
        session.Events.StartStream<Order>(cmd.OrderId, evt);
        await bus.PublishAsync(evt);
        // Wolverine AutoApplyTransactions calls SaveChangesAsync here
        return new PlaceOrderResult(cmd.OrderId);
    }
}
```

- [ ] **Step 2: Write src/Order/Order.Api/Handlers/Events/StockReservationFailedHandler.cs**

```csharp
// src/Order/Order.Api/Handlers/Events/StockReservationFailedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class StockReservationFailedHandler
{
    public static async Task Handle(
        StockReservationFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var failed = new OrderFailed(evt.OrderId, evt.Reason);
        session.Events.Append(evt.OrderId, failed);
        await bus.PublishAsync(failed);
    }
}
```

- [ ] **Step 3: Write src/Order/Order.Api/Handlers/Events/PaymentProcessedHandler.cs**

```csharp
// src/Order/Order.Api/Handlers/Events/PaymentProcessedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class PaymentProcessedHandler
{
    public static async Task Handle(
        PaymentProcessed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var confirmed = new OrderConfirmed(evt.OrderId, DateTimeOffset.UtcNow);
        session.Events.Append(evt.OrderId, confirmed);
        await bus.PublishAsync(confirmed);
    }
}
```

- [ ] **Step 4: Write src/Order/Order.Api/Handlers/Events/PaymentFailedHandler.cs**

```csharp
// src/Order/Order.Api/Handlers/Events/PaymentFailedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class PaymentFailedHandler
{
    public static async Task Handle(
        PaymentFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var failed = new OrderFailed(evt.OrderId, $"Payment failed: {evt.Reason}");
        session.Events.Append(evt.OrderId, failed);
        await bus.PublishAsync(failed);
    }
}
```

- [ ] **Step 5: Verify build**

```bash
dotnet build src/Order/Order.Api/Order.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/Order/Order.Api/Handlers/
git commit -m "feat: add Order command handler and event handlers"
```

---

## Task 12: Order.Api — HTTP Endpoints

**Files:**
- Create: `src/Order/Order.Api/Endpoints/OrderEndpoints.cs`

- [ ] **Step 1: Write src/Order/Order.Api/Endpoints/OrderEndpoints.cs**

```csharp
// src/Order/Order.Api/Endpoints/OrderEndpoints.cs
using Order.Api.Commands;
using Wolverine;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "order-api", status = "running" }));

        app.MapPost("/api/orders", async (PlaceOrderRequest req, IMessageBus bus) =>
        {
            if (req.Items is null || req.Items.Length == 0)
                return Results.BadRequest("Order must contain at least one item.");

            var total = req.Items.Sum(i => i.UnitPrice * i.Quantity);
            var cmd = new PlaceOrderCommand(Guid.NewGuid(), req.UserId, req.Items, total);
            var result = await bus.InvokeAsync<PlaceOrderResult>(cmd);
            return Results.Accepted($"/api/orders/{result.OrderId}", result);
        });

        return app;
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/Order/Order.Api/Order.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Order/Order.Api/Endpoints/
git commit -m "feat: add Order.Api POST /api/orders endpoint"
```

---

## Task 13: Payment.Api — Setup, Domain & Handler

**Files:**
- Modify: `src/Payment/Payment.Api/Payment.Api.csproj`
- Create: `src/Payment/Payment.Api/Program.cs`
- Create: `src/Payment/Payment.Api/Domain/Payment.cs`
- Create: `src/Payment/Payment.Api/Handlers/Events/StockReservedHandler.cs`
- Create: `src/Payment/Payment.Api/Endpoints/PaymentEndpoints.cs`
- Create: `tests/Payment.Api.Tests/Domain/PaymentTests.cs`

- [ ] **Step 1: Add NuGet packages**

```bash
dotnet add src/Payment/Payment.Api/Payment.Api.csproj package Marten
dotnet add src/Payment/Payment.Api/Payment.Api.csproj package WolverineFx
dotnet add src/Payment/Payment.Api/Payment.Api.csproj package WolverineFx.Marten
dotnet add src/Payment/Payment.Api/Payment.Api.csproj package WolverineFx.RabbitMQ
dotnet add src/Payment/Payment.Api/Payment.Api.csproj package Aspire.Npgsql
```

- [ ] **Step 2: Write failing domain tests**

```csharp
// tests/Payment.Api.Tests/Domain/PaymentTests.cs
using Contracts.Events;
using Contracts.Models;

namespace Payment.Api.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Apply_PaymentProcessed_SetsProcessedStatus()
    {
        var payment = new Payment.Api.Domain.Payment();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        payment.Apply(new PaymentProcessed(paymentId, orderId, 899.99m));
        Assert.Equal(PaymentStatus.Processed, payment.Status);
        Assert.Equal(899.99m, payment.Amount);
    }

    [Fact]
    public void Apply_PaymentFailed_SetsFailedStatus()
    {
        var payment = new Payment.Api.Domain.Payment();
        var items = new[] { new OrderItem(Guid.NewGuid(), "Bauer Mask", "Bauer", 1, 899.99m) };
        payment.Apply(new PaymentFailed(Guid.NewGuid(), Guid.NewGuid(), "Card declined", items));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason);
    }
}
```

- [ ] **Step 3: Run tests — verify they fail**

```bash
dotnet test tests/Payment.Api.Tests/Payment.Api.Tests.csproj --filter "FullyQualifiedName~PaymentTests"
```

Expected: FAIL.

- [ ] **Step 4: Implement src/Payment/Payment.Api/Domain/Payment.cs**

```csharp
// src/Payment/Payment.Api/Domain/Payment.cs
using Contracts.Events;

namespace Payment.Api.Domain;

public enum PaymentStatus { Pending, Processed, Failed }

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void Apply(PaymentProcessed evt)
    {
        Id = evt.PaymentId;
        OrderId = evt.OrderId;
        Amount = evt.Amount;
        Status = PaymentStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Apply(PaymentFailed evt)
    {
        Id = evt.PaymentId;
        OrderId = evt.OrderId;
        Status = PaymentStatus.Failed;
        FailureReason = evt.Reason;
    }
}
```

- [ ] **Step 5: Run tests — verify they pass**

```bash
dotnet test tests/Payment.Api.Tests/Payment.Api.Tests.csproj --filter "FullyQualifiedName~PaymentTests"
```

Expected: PASS — 2 tests.

- [ ] **Step 6: Implement StockReservedHandler**

```csharp
// src/Payment/Payment.Api/Handlers/Events/StockReservedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Payment.Api.Handlers.Events;

public static class StockReservedHandler
{
    public static async Task Handle(
        StockReserved evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var paymentId = Guid.NewGuid();
        var total = evt.Items.Sum(i => i.UnitPrice * i.Quantity);

        // Simulate payment processing — always succeeds in this POC
        // To test failure path: randomly fail 20% of the time
        // if (Random.Shared.NextDouble() < 0.2) { ... }

        var processedEvt = new PaymentProcessed(paymentId, evt.OrderId, total);
        session.Events.StartStream<Payment>(paymentId, processedEvt);
        await bus.PublishAsync(processedEvt);
    }
}
```

- [ ] **Step 7: Implement Program.cs**

```csharp
// src/Payment/Payment.Api/Program.cs
using Contracts.Events;
using Payment.Api.Endpoints;
using Marten;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("payments-db")!);
    opts.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .AutoPurgeOnStartup();

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.PublishMessage<PaymentProcessed>().ToRabbitExchange("payment-events");
    opts.PublishMessage<PaymentFailed>().ToRabbitExchange("payment-events");

    opts.ListenToRabbitQueue("payment-api-inbox")
        .BindExchange("inventory-events");  // receives StockReserved
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseDefaultExceptionHandler();
app.UseCors();
app.MapDefaultEndpoints();
app.MapPaymentEndpoints();
app.Run();
```

- [ ] **Step 8: Implement PaymentEndpoints**

```csharp
// src/Payment/Payment.Api/Endpoints/PaymentEndpoints.cs
namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "payment-api", status = "running" }));
        return app;
    }
}
```

- [ ] **Step 9: Verify build**

```bash
dotnet build src/Payment/Payment.Api/Payment.Api.csproj
```

Expected: Build succeeded.

- [ ] **Step 10: Commit**

```bash
git add src/Payment/ tests/Payment.Api.Tests/
git commit -m "feat: add Payment.Api with domain, StockReserved handler, and fake payment processing"
```

---

## Task 14: ReadModel.Projector — EF Core Setup

**Files:**
- Modify: `src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj`
- Create: `src/ReadModel/ReadModel.Projector/Models/OrderReadModel.cs`
- Create: `src/ReadModel/ReadModel.Projector/Models/OrderLineReadModel.cs`
- Create: `src/ReadModel/ReadModel.Projector/Models/InventoryItemReadModel.cs`
- Create: `src/ReadModel/ReadModel.Projector/Data/AppDbContext.cs`

- [ ] **Step 1: Add NuGet packages**

```bash
dotnet add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj package WolverineFx
dotnet add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj package WolverineFx.RabbitMQ
dotnet add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Create Models/InventoryItemReadModel.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Models/InventoryItemReadModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadModel.Projector.Models;

[Table("inventory_items")]
public class InventoryItemReadModel
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Brand { get; set; } = "";

    [Required, MaxLength(100)]
    public string Category { get; set; } = "";

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public int StockQty { get; set; }
}
```

- [ ] **Step 3: Create Models/OrderReadModel.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Models/OrderReadModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadModel.Projector.Models;

[Table("orders")]
public class OrderReadModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? FailureReason { get; set; }

    public ICollection<OrderLineReadModel> Lines { get; set; } = [];
}
```

- [ ] **Step 4: Create Models/OrderLineReadModel.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Models/OrderLineReadModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadModel.Projector.Models;

[Table("order_lines")]
public class OrderLineReadModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderReadModel Order { get; set; } = null!;

    public Guid ItemId { get; set; }

    [Required, MaxLength(200)]
    public string ItemName { get; set; } = "";

    [Required, MaxLength(100)]
    public string Brand { get; set; } = "";

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
}
```

- [ ] **Step 5: Create Data/AppDbContext.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Models;

namespace ReadModel.Projector.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItemReadModel> InventoryItems => Set<InventoryItemReadModel>();
    public DbSet<OrderReadModel> Orders => Set<OrderReadModel>();
    public DbSet<OrderLineReadModel> OrderLines => Set<OrderLineReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderReadModel>()
            .HasMany(o => o.Lines)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId);
    }
}
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj
```

Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add src/ReadModel/ReadModel.Projector/Models/ src/ReadModel/ReadModel.Projector/Data/
git commit -m "feat: add ReadModel EF Core models and DbContext"
```

---

## Task 15: ReadModel.Projector — Wolverine Setup & Program.cs

**Files:**
- Create: `src/ReadModel/ReadModel.Projector/Program.cs`

- [ ] **Step 1: Write src/ReadModel/ReadModel.Projector/Program.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Program.cs
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;
using ReadModel.Projector.Endpoints;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── EF Core (read model database) ──────────────────────────
builder.AddNpgsqlDbContext<AppDbContext>("readmodel-db");

// ── Wolverine (receive events from RabbitMQ) ─────────────────
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .AutoPurgeOnStartup();

    // Projector does not publish messages — only listens
    opts.ListenToRabbitQueue("projector-inbox")
        .BindExchange("order-events")
        .BindExchange("inventory-events")
        .BindExchange("payment-events");
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate the read model database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseDefaultExceptionHandler();
app.UseCors();
app.MapDefaultEndpoints();
app.MapCatalogEndpoints();
app.Run();
```

- [ ] **Step 2: Generate EF Core initial migration**

```bash
cd src/ReadModel/ReadModel.Projector
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
cd ../../..
```

Expected: Migration files created in `Data/Migrations/`.

- [ ] **Step 3: Verify build**

```bash
dotnet build src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/ReadModel/ReadModel.Projector/
git commit -m "feat: configure ReadModel.Projector with Wolverine, EF Core, and initial migration"
```

---

## Task 16: ReadModel.Projector — Event Handlers

**Files:**
- Create: `src/ReadModel/ReadModel.Projector/Handlers/OrderProjection.cs`
- Create: `src/ReadModel/ReadModel.Projector/Handlers/InventoryProjection.cs`

- [ ] **Step 1: Create Handlers/OrderProjection.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Handlers/OrderProjection.cs
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;
using ReadModel.Projector.Models;

namespace ReadModel.Projector.Handlers;

public static class OrderProjection
{
    public static async Task Handle(OrderPlaced evt, AppDbContext db)
    {
        var order = new OrderReadModel
        {
            Id = evt.OrderId,
            UserId = evt.UserId,
            Status = "Pending",
            TotalAmount = evt.Total,
            PlacedAt = DateTimeOffset.UtcNow,
        };

        foreach (var item in evt.Items)
        {
            order.Lines.Add(new OrderLineReadModel
            {
                Id = Guid.NewGuid(),
                OrderId = evt.OrderId,
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Brand = item.Brand,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public static async Task Handle(OrderConfirmed evt, AppDbContext db)
    {
        await db.Orders
            .Where(o => o.Id == evt.OrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, "Confirmed")
                .SetProperty(o => o.ConfirmedAt, evt.ConfirmedAt));
    }

    public static async Task Handle(OrderFailed evt, AppDbContext db)
    {
        await db.Orders
            .Where(o => o.Id == evt.OrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, "Failed")
                .SetProperty(o => o.FailureReason, evt.Reason));
    }
}
```

- [ ] **Step 2: Create Handlers/InventoryProjection.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Handlers/InventoryProjection.cs
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;
using ReadModel.Projector.Models;

namespace ReadModel.Projector.Handlers;

public static class InventoryProjection
{
    public static async Task Handle(InventoryItemCreated evt, AppDbContext db)
    {
        // Idempotent — skip if already exists (seeder may fire multiple times in dev)
        if (await db.InventoryItems.AnyAsync(i => i.Id == evt.ItemId)) return;

        db.InventoryItems.Add(new InventoryItemReadModel
        {
            Id = evt.ItemId,
            Brand = evt.Brand,
            Category = evt.Category,
            Name = evt.Name,
            Description = evt.Description,
            Price = evt.Price,
            StockQty = evt.InitialStock,
        });
        await db.SaveChangesAsync();
    }

    public static async Task Handle(StockQuantityUpdated evt, AppDbContext db)
    {
        await db.InventoryItems
            .Where(i => i.Id == evt.ItemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.StockQty, evt.NewQuantity));
    }
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/ReadModel/ReadModel.Projector/Handlers/
git commit -m "feat: add ReadModel event projections for orders and inventory"
```

---

## Task 17: ReadModel.Projector — HTTP Query Endpoints

**Files:**
- Create: `src/ReadModel/ReadModel.Projector/Endpoints/CatalogEndpoints.cs`

- [ ] **Step 1: Create Endpoints/CatalogEndpoints.cs**

```csharp
// src/ReadModel/ReadModel.Projector/Endpoints/CatalogEndpoints.cs
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;

namespace ReadModel.Projector.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "readmodel-projector", status = "running" }));

        // GET /api/catalog/items — all inventory items
        app.MapGet("/api/catalog/items", async (AppDbContext db) =>
        {
            var items = await db.InventoryItems
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Brand)
                .Select(i => new
                {
                    i.Id,
                    i.Brand,
                    i.Category,
                    i.Name,
                    i.Description,
                    i.Price,
                    i.StockQty,
                })
                .ToListAsync();
            return Results.Ok(items);
        });

        // GET /api/catalog/orders?userId={userId} — orders for a user
        app.MapGet("/api/catalog/orders", async (Guid userId, AppDbContext db) =>
        {
            var orders = await db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.PlacedAt)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.PlacedAt,
                    o.ConfirmedAt,
                    o.FailureReason,
                })
                .ToListAsync();
            return Results.Ok(orders);
        });

        // GET /api/catalog/orders/{orderId} — single order with lines
        app.MapGet("/api/catalog/orders/{orderId:guid}", async (Guid orderId, AppDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Lines)
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.PlacedAt,
                    o.ConfirmedAt,
                    o.FailureReason,
                    Lines = o.Lines.Select(l => new
                    {
                        l.Id,
                        l.ItemId,
                        l.ItemName,
                        l.Brand,
                        l.Quantity,
                        l.UnitPrice,
                    }),
                })
                .FirstOrDefaultAsync();

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        return app;
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/ReadModel/ReadModel.Projector/ReadModel.Projector.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Run a full solution build to catch cross-project issues**

```bash
dotnet build OrderDemo.slnx
```

Expected: Build succeeded across all projects.

- [ ] **Step 4: Commit**

```bash
git add src/ReadModel/ReadModel.Projector/Endpoints/
git commit -m "feat: add ReadModel HTTP catalog query endpoints"
```

---

## Task 18: Frontend — Scaffold & Configure

**Files:**
- Create: `src/Frontend/frontend/` (Vite scaffold)
- Create: `src/Frontend/frontend/vite.config.ts`
- Create: `src/Frontend/frontend/src/types/index.ts`
- Create: `src/Frontend/frontend/src/lib/users.ts`
- Create: `src/Frontend/frontend/src/lib/api.ts`

- [ ] **Step 1: Scaffold Vite React TypeScript project**

```bash
cd src/Frontend
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

- [ ] **Step 2: Install dependencies**

```bash
npm install @tanstack/react-query react-router-dom
npm install -D tailwindcss @tailwindcss/vite
npx shadcn@latest init
```

When `shadcn init` prompts:
- Style: Default
- Base colour: Slate
- CSS variables: Yes

Then add the components we need:
```bash
npx shadcn@latest add button card badge input label select separator
```

- [ ] **Step 3: Write vite.config.ts**

```typescript
// src/Frontend/frontend/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 3000,
    proxy: {
      '/api/orders': {
        target: 'https://localhost:7002',
        changeOrigin: true,
        secure: false,
      },
      '/api/catalog': {
        target: 'https://localhost:7004',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

- [ ] **Step 4: Write src/types/index.ts**

```typescript
// src/Frontend/frontend/src/types/index.ts
export interface InventoryItem {
  id: string
  brand: string
  category: string
  name: string
  description: string
  price: number
  stockQty: number
}

export interface OrderItem {
  itemId: string
  itemName: string
  brand: string
  quantity: number
  unitPrice: number
}

export interface CartItem extends InventoryItem {
  quantity: number
}

export interface OrderLine {
  id: string
  itemId: string
  itemName: string
  brand: string
  quantity: number
  unitPrice: number
}

export interface Order {
  id: string
  userId: string
  status: 'Pending' | 'Confirmed' | 'Failed'
  totalAmount: number
  placedAt: string
  confirmedAt?: string
  failureReason?: string
  lines?: OrderLine[]
}

export interface User {
  id: string
  name: string
}
```

- [ ] **Step 5: Write src/lib/users.ts**

```typescript
// src/Frontend/frontend/src/lib/users.ts
import { User } from '@/types'

export const USERS: User[] = [
  { id: 'usr-001', name: 'Mike Smith' },
  { id: 'usr-002', name: 'Sara Jones' },
  { id: 'usr-003', name: 'Dave Lee' },
]

const USER_KEY = 'orderdemo_user_id'

export function getCurrentUser(): User {
  const id = localStorage.getItem(USER_KEY) ?? USERS[0].id
  return USERS.find(u => u.id === id) ?? USERS[0]
}

export function setCurrentUser(userId: string): void {
  localStorage.setItem(USER_KEY, userId)
}
```

- [ ] **Step 6: Write src/lib/api.ts**

```typescript
// src/Frontend/frontend/src/lib/api.ts
import { InventoryItem, Order, OrderItem } from '@/types'

async function get<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) throw new Error(`GET ${url} → ${res.status}`)
  return res.json()
}

async function post<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`POST ${url} → ${res.status}`)
  return res.json()
}

export const api = {
  getItems: (): Promise<InventoryItem[]> =>
    get('/api/catalog/items'),

  getOrders: (userId: string): Promise<Order[]> =>
    get(`/api/catalog/orders?userId=${userId}`),

  getOrder: (orderId: string): Promise<Order> =>
    get(`/api/catalog/orders/${orderId}`),

  placeOrder: (userId: string, items: OrderItem[]): Promise<{ orderId: string }> =>
    post('/api/orders', { userId, items }),
}
```

- [ ] **Step 7: Verify dev server starts**

```bash
cd src/Frontend/frontend
npm run dev
```

Expected: Vite dev server starts on http://localhost:3000.

- [ ] **Step 8: Commit**

```bash
cd ../../..
git add src/Frontend/
git commit -m "feat: scaffold React frontend with Vite, shadcn/ui, and API client"
```

---

## Task 19: Frontend — Layout, Navigation & Cart Hook

**Files:**
- Create: `src/Frontend/frontend/src/hooks/useCart.ts`
- Create: `src/Frontend/frontend/src/components/UserSwitcher.tsx`
- Create: `src/Frontend/frontend/src/components/NavBar.tsx`
- Create: `src/Frontend/frontend/src/components/OrderStatusBadge.tsx`
- Create: `src/Frontend/frontend/src/App.tsx`
- Create: `src/Frontend/frontend/src/main.tsx`

- [ ] **Step 1: Write src/hooks/useCart.ts**

```typescript
// src/Frontend/frontend/src/hooks/useCart.ts
import { useState } from 'react'
import { CartItem, InventoryItem } from '@/types'

export function useCart() {
  const [items, setItems] = useState<CartItem[]>([])

  const addItem = (item: InventoryItem) => {
    setItems(prev => {
      const existing = prev.find(i => i.id === item.id)
      if (existing) {
        return prev.map(i => i.id === item.id ? { ...i, quantity: i.quantity + 1 } : i)
      }
      return [...prev, { ...item, quantity: 1 }]
    })
  }

  const removeItem = (itemId: string) =>
    setItems(prev => prev.filter(i => i.id !== itemId))

  const clearCart = () => setItems([])

  const total = items.reduce((sum, i) => sum + i.price * i.quantity, 0)

  return { items, addItem, removeItem, clearCart, total }
}
```

- [ ] **Step 2: Write src/components/UserSwitcher.tsx**

```typescript
// src/Frontend/frontend/src/components/UserSwitcher.tsx
import { USERS, getCurrentUser, setCurrentUser } from '@/lib/users'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

export function UserSwitcher({ onChange }: { onChange: () => void }) {
  const current = getCurrentUser()

  return (
    <Select
      defaultValue={current.id}
      onValueChange={id => { setCurrentUser(id); onChange() }}
    >
      <SelectTrigger className="w-40">
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {USERS.map(u => (
          <SelectItem key={u.id} value={u.id}>{u.name}</SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
```

- [ ] **Step 3: Write src/components/OrderStatusBadge.tsx**

```typescript
// src/Frontend/frontend/src/components/OrderStatusBadge.tsx
import { Badge } from '@/components/ui/badge'

const variants: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  Pending: 'secondary',
  Confirmed: 'default',
  Failed: 'destructive',
}

export function OrderStatusBadge({ status }: { status: string }) {
  return <Badge variant={variants[status] ?? 'outline'}>{status}</Badge>
}
```

- [ ] **Step 4: Write src/components/NavBar.tsx**

```typescript
// src/Frontend/frontend/src/components/NavBar.tsx
import { Link } from 'react-router-dom'
import { UserSwitcher } from './UserSwitcher'
import { ShoppingCart } from 'lucide-react'

export function NavBar({ cartCount, onUserChange }: {
  cartCount: number
  onUserChange: () => void
}) {
  return (
    <nav className="border-b bg-background px-6 py-3 flex items-center justify-between">
      <Link to="/" className="font-bold text-xl tracking-tight">
        ⛸ GoalieGear
      </Link>
      <div className="flex items-center gap-4">
        <Link to="/orders" className="text-sm text-muted-foreground hover:text-foreground">
          My Orders
        </Link>
        <Link to="/checkout" className="relative flex items-center gap-1 text-sm">
          <ShoppingCart className="h-5 w-5" />
          {cartCount > 0 && (
            <span className="absolute -top-2 -right-2 bg-primary text-primary-foreground text-xs rounded-full h-4 w-4 flex items-center justify-center">
              {cartCount}
            </span>
          )}
        </Link>
        <UserSwitcher onChange={onUserChange} />
      </div>
    </nav>
  )
}
```

- [ ] **Step 5: Write src/App.tsx**

```typescript
// src/Frontend/frontend/src/App.tsx
import { useState } from 'react'
import { Routes, Route } from 'react-router-dom'
import { NavBar } from '@/components/NavBar'
import { ShopPage } from '@/pages/ShopPage'
import { CheckoutPage } from '@/pages/CheckoutPage'
import { OrdersPage } from '@/pages/OrdersPage'
import { OrderDetailPage } from '@/pages/OrderDetailPage'
import { useCart } from '@/hooks/useCart'

export default function App() {
  const cart = useCart()
  const [, forceRender] = useState(0)

  return (
    <div className="min-h-screen bg-background">
      <NavBar
        cartCount={cart.items.reduce((n, i) => n + i.quantity, 0)}
        onUserChange={() => forceRender(n => n + 1)}
      />
      <main className="container mx-auto px-4 py-6">
        <Routes>
          <Route path="/" element={<ShopPage cart={cart} />} />
          <Route path="/checkout" element={<CheckoutPage cart={cart} />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/orders/:id" element={<OrderDetailPage />} />
        </Routes>
      </main>
    </div>
  )
}
```

- [ ] **Step 6: Write src/main.tsx**

```typescript
// src/Frontend/frontend/src/main.tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App.tsx'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </BrowserRouter>
  </StrictMode>,
)
```

- [ ] **Step 7: Commit**

```bash
git add src/Frontend/frontend/src/
git commit -m "feat: add frontend layout, navigation, user switcher, and cart hook"
```

---

## Task 20: Frontend — Shop Page

**Files:**
- Create: `src/Frontend/frontend/src/components/ProductCard.tsx`
- Create: `src/Frontend/frontend/src/pages/ShopPage.tsx`

- [ ] **Step 1: Write src/components/ProductCard.tsx**

```typescript
// src/Frontend/frontend/src/components/ProductCard.tsx
import { InventoryItem } from '@/types'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

export function ProductCard({
  item,
  onAddToCart,
}: {
  item: InventoryItem
  onAddToCart: (item: InventoryItem) => void
}) {
  const outOfStock = item.stockQty === 0

  return (
    <Card className="flex flex-col">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-base leading-tight">{item.name}</CardTitle>
          <Badge variant="outline" className="shrink-0">{item.brand}</Badge>
        </div>
      </CardHeader>
      <CardContent className="flex-1">
        <p className="text-sm text-muted-foreground">{item.description}</p>
        <p className="mt-2 text-xs text-muted-foreground">
          {outOfStock ? (
            <span className="text-destructive font-medium">Out of stock</span>
          ) : (
            <span>{item.stockQty} in stock</span>
          )}
        </p>
      </CardContent>
      <CardFooter className="flex items-center justify-between">
        <span className="font-semibold text-lg">${item.price.toFixed(2)}</span>
        <Button
          size="sm"
          disabled={outOfStock}
          onClick={() => onAddToCart(item)}
        >
          Add to Cart
        </Button>
      </CardFooter>
    </Card>
  )
}
```

- [ ] **Step 2: Write src/pages/ShopPage.tsx**

```typescript
// src/Frontend/frontend/src/pages/ShopPage.tsx
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { InventoryItem } from '@/types'
import { ProductCard } from '@/components/ProductCard'
import { useCart } from '@/hooks/useCart'
import { Button } from '@/components/ui/button'

const BRANDS = ['All', 'Bauer', 'CCM', 'True']
const CATEGORIES = ['All', 'Mask', 'Pads', 'Glove & Blocker', 'Chest & Arms']

export function ShopPage({ cart }: { cart: ReturnType<typeof useCart> }) {
  const [brand, setBrand] = useState('All')
  const [category, setCategory] = useState('All')

  const { data: items = [], isLoading, error } = useQuery({
    queryKey: ['items'],
    queryFn: api.getItems,
  })

  const filtered = items.filter((i: InventoryItem) =>
    (brand === 'All' || i.brand === brand) &&
    (category === 'All' || i.category === category)
  )

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading gear...</div>
  if (error) return <div className="text-center py-20 text-destructive">Failed to load products.</div>

  return (
    <div>
      <h1 className="text-2xl font-bold mb-4">Goalie Equipment</h1>

      {/* Filters */}
      <div className="flex flex-wrap gap-2 mb-6">
        <div className="flex gap-1">
          {BRANDS.map(b => (
            <Button
              key={b}
              size="sm"
              variant={brand === b ? 'default' : 'outline'}
              onClick={() => setBrand(b)}
            >{b}</Button>
          ))}
        </div>
        <div className="flex gap-1">
          {CATEGORIES.map(c => (
            <Button
              key={c}
              size="sm"
              variant={category === c ? 'default' : 'outline'}
              onClick={() => setCategory(c)}
            >{c}</Button>
          ))}
        </div>
      </div>

      {/* Product grid */}
      {filtered.length === 0 ? (
        <p className="text-muted-foreground">No products match the selected filters.</p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {filtered.map((item: InventoryItem) => (
            <ProductCard key={item.id} item={item} onAddToCart={cart.addItem} />
          ))}
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Frontend/frontend/src/components/ProductCard.tsx src/Frontend/frontend/src/pages/ShopPage.tsx
git commit -m "feat: add Shop page with product grid and brand/category filters"
```

---

## Task 21: Frontend — Checkout Page

**Files:**
- Create: `src/Frontend/frontend/src/pages/CheckoutPage.tsx`

- [ ] **Step 1: Write src/pages/CheckoutPage.tsx**

```typescript
// src/Frontend/frontend/src/pages/CheckoutPage.tsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/lib/api'
import { getCurrentUser } from '@/lib/users'
import { useCart } from '@/hooks/useCart'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'

export function CheckoutPage({ cart }: { cart: ReturnType<typeof useCart> }) {
  const navigate = useNavigate()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Fake payment fields — no real validation
  const [card, setCard] = useState({ name: '', number: '', expiry: '', cvv: '' })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (cart.items.length === 0) return

    setSubmitting(true)
    setError(null)

    try {
      const user = getCurrentUser()
      const orderItems = cart.items.map(i => ({
        itemId: i.id,
        itemName: i.name,
        brand: i.brand,
        quantity: i.quantity,
        unitPrice: i.price,
      }))

      const result = await api.placeOrder(user.id, orderItems)
      cart.clearCart()
      navigate(`/orders/${result.orderId}`)
    } catch {
      setError('Failed to place order. Please try again.')
      setSubmitting(false)
    }
  }

  if (cart.items.length === 0) {
    return (
      <div className="text-center py-20">
        <p className="text-muted-foreground">Your cart is empty.</p>
        <Button className="mt-4" onClick={() => navigate('/')}>Browse Gear</Button>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Checkout</h1>

      {/* Order summary */}
      <Card>
        <CardHeader><CardTitle>Order Summary</CardTitle></CardHeader>
        <CardContent className="space-y-2">
          {cart.items.map(item => (
            <div key={item.id} className="flex justify-between text-sm">
              <span>{item.name} × {item.quantity}</span>
              <span>${(item.price * item.quantity).toFixed(2)}</span>
            </div>
          ))}
          <Separator />
          <div className="flex justify-between font-semibold">
            <span>Total</span>
            <span>${cart.total.toFixed(2)}</span>
          </div>
        </CardContent>
      </Card>

      {/* Fake payment form */}
      <Card>
        <CardHeader><CardTitle>Payment Details</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <Label>Cardholder Name</Label>
              <Input
                placeholder="Wayne Gretzky"
                value={card.name}
                onChange={e => setCard(c => ({ ...c, name: e.target.value }))}
                required
              />
            </div>
            <div className="space-y-1">
              <Label>Card Number (any 16 digits)</Label>
              <Input
                placeholder="4111 1111 1111 1111"
                value={card.number}
                onChange={e => setCard(c => ({ ...c, number: e.target.value }))}
                maxLength={19}
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <Label>Expiry</Label>
                <Input
                  placeholder="12/28"
                  value={card.expiry}
                  onChange={e => setCard(c => ({ ...c, expiry: e.target.value }))}
                  required
                />
              </div>
              <div className="space-y-1">
                <Label>CVV</Label>
                <Input
                  placeholder="123"
                  value={card.cvv}
                  onChange={e => setCard(c => ({ ...c, cvv: e.target.value }))}
                  maxLength={4}
                  required
                />
              </div>
            </div>
            {error && <p className="text-destructive text-sm">{error}</p>}
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? 'Placing Order...' : `Place Order — $${cart.total.toFixed(2)}`}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Frontend/frontend/src/pages/CheckoutPage.tsx
git commit -m "feat: add Checkout page with order summary and fake payment form"
```

---

## Task 22: Frontend — Orders Pages

**Files:**
- Create: `src/Frontend/frontend/src/pages/OrdersPage.tsx`
- Create: `src/Frontend/frontend/src/pages/OrderDetailPage.tsx`

- [ ] **Step 1: Write src/pages/OrdersPage.tsx**

```typescript
// src/Frontend/frontend/src/pages/OrdersPage.tsx
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '@/lib/api'
import { getCurrentUser } from '@/lib/users'
import { Order } from '@/types'
import { OrderStatusBadge } from '@/components/OrderStatusBadge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'

export function OrdersPage() {
  const user = getCurrentUser()

  const { data: orders = [], isLoading } = useQuery<Order[]>({
    queryKey: ['orders', user.id],
    queryFn: () => api.getOrders(user.id),
    refetchInterval: 5000,   // poll every 5s to show Pending → Confirmed transition
  })

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading orders...</div>

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">My Orders — {user.name}</h1>
        <Link to="/"><Button variant="outline" size="sm">Continue Shopping</Button></Link>
      </div>

      {orders.length === 0 ? (
        <p className="text-muted-foreground text-center py-20">No orders yet.</p>
      ) : (
        <div className="space-y-3">
          {orders.map((order: Order) => (
            <Link key={order.id} to={`/orders/${order.id}`}>
              <Card className="hover:bg-muted/50 transition-colors cursor-pointer">
                <CardContent className="flex items-center justify-between py-4">
                  <div>
                    <p className="font-medium text-sm font-mono">{order.id}</p>
                    <p className="text-xs text-muted-foreground">
                      {new Date(order.placedAt).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="font-semibold">${order.totalAmount.toFixed(2)}</span>
                    <OrderStatusBadge status={order.status} />
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Write src/pages/OrderDetailPage.tsx**

```typescript
// src/Frontend/frontend/src/pages/OrderDetailPage.tsx
import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Order } from '@/types'
import { OrderStatusBadge } from '@/components/OrderStatusBadge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: order, isLoading } = useQuery<Order>({
    queryKey: ['order', id],
    queryFn: () => api.getOrder(id!),
    refetchInterval: order => order?.status === 'Pending' ? 3000 : false,
  })

  if (isLoading) return <div className="text-center py-20 text-muted-foreground">Loading order...</div>
  if (!order) return <div className="text-center py-20 text-destructive">Order not found.</div>

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/orders"><Button variant="outline" size="sm">← My Orders</Button></Link>
        <h1 className="text-2xl font-bold">Order Detail</h1>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base font-mono">{order.id}</CardTitle>
            <OrderStatusBadge status={order.status} />
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="text-sm text-muted-foreground space-y-1">
            <p>Placed: {new Date(order.placedAt).toLocaleString()}</p>
            {order.confirmedAt && <p>Confirmed: {new Date(order.confirmedAt).toLocaleString()}</p>}
            {order.failureReason && (
              <p className="text-destructive">Reason: {order.failureReason}</p>
            )}
          </div>

          <Separator />

          {/* Line items */}
          <div className="space-y-2">
            {order.lines?.map(line => (
              <div key={line.id} className="flex justify-between text-sm">
                <span>
                  <span className="font-medium">{line.itemName}</span>
                  <span className="text-muted-foreground"> × {line.quantity}</span>
                </span>
                <span>${(line.unitPrice * line.quantity).toFixed(2)}</span>
              </div>
            ))}
          </div>

          <Separator />

          <div className="flex justify-between font-semibold">
            <span>Total</span>
            <span>${order.totalAmount.toFixed(2)}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
```

- [ ] **Step 3: Commit**

```bash
git add src/Frontend/frontend/src/pages/
git commit -m "feat: add Orders list and Order Detail pages with auto-refresh"
```

---

## Task 23: End-to-End Smoke Test

This task verifies the full system runs together.

- [ ] **Step 1: Ensure Docker is running**

```bash
docker info
```

Expected: Docker daemon info. If not running, start Docker Desktop.

- [ ] **Step 2: Start the full stack**

```bash
dotnet run --project src/AppHost/AppHost.csproj
```

Expected: Aspire dashboard opens at https://localhost:15888. All resources show green after ~30 seconds.

- [ ] **Step 3: Verify Postgres and RabbitMQ are healthy**

Check Aspire dashboard:
- `postgres` → Running
- `rabbitmq` → Running
- RabbitMQ management UI at http://localhost:15672 (guest/guest) → shows exchanges: `order-events`, `inventory-events`, `payment-events`

- [ ] **Step 4: Verify services are healthy**

```bash
curl https://localhost:7001/health  # inventory-api
curl https://localhost:7002/health  # order-api
curl https://localhost:7003/health  # payment-api
curl https://localhost:7004/health  # readmodel-projector
```

Expected: all return `{"status":"Healthy"}`.

- [ ] **Step 5: Verify seeded items appear in read model**

```bash
curl https://localhost:7004/api/catalog/items | jq '. | length'
```

Expected: `12`

- [ ] **Step 6: Place a test order via curl**

```bash
curl -X POST https://localhost:7002/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "a0000000-0000-0000-0000-000000000001",
    "items": [{
      "itemId": "<paste an itemId from step 5>",
      "itemName": "Bauer Hyperlite2 Goalie Mask",
      "brand": "Bauer",
      "quantity": 1,
      "unitPrice": 899.99
    }]
  }'
```

Expected: HTTP 202 with `{ "orderId": "<guid>" }`

- [ ] **Step 7: Watch order status move to Confirmed**

```bash
# Wait 2-3 seconds for choreography to complete, then:
curl "https://localhost:7004/api/catalog/orders?userId=a0000000-0000-0000-0000-000000000001" | jq '.[0].status'
```

Expected: `"Confirmed"` (after a brief delay while Inventory + Payment handlers process).

- [ ] **Step 8: Verify frontend**

Open http://localhost:3000 in a browser.
- Shop page loads with 12 products in a grid
- Add items to cart, proceed to checkout
- Fill fake payment details, place order
- Redirect to Order Detail — status shows Pending, then refreshes to Confirmed within seconds
- My Orders page shows the order with correct status badge

- [ ] **Step 9: Final commit**

```bash
git add .
git commit -m "chore: complete end-to-end smoke test verification"
```

---

## Self-Review Checklist

- [x] Contracts project — all events defined, plain records, no framework deps
- [x] AppHost — static ports, named volumes, all services registered with WaitFor
- [x] Inventory.Api — Marten + Wolverine wired, seeder, OrderPlaced handler, PaymentFailed compensation
- [x] Order.Api — PlaceOrder command handler, all incoming event handlers (StockReservationFailed, PaymentProcessed, PaymentFailed)
- [x] Payment.Api — StockReserved handler, fake payment always succeeds
- [x] ReadModel.Projector — EF Core, migrations, order + inventory projections, catalog HTTP endpoints
- [x] Frontend — Shop, Checkout, Orders, Order Detail pages + cart + user switcher
- [x] RabbitMQ exchange routing correct — Inventory.Api subscribes to payment-events for compensation
- [x] API path prefixes distinct — `/api/orders` (write) vs `/api/catalog` (read)
- [x] `StockReserved` and `PaymentFailed` both carry `OrderItem[]` so compensation handlers have quantities
- [x] No hardcoded connection strings — all via Aspire `GetConnectionString()`
- [x] `AutoPurgeOnStartup()` on all services for clean dev restarts
