# Critterstack Order Demo — Design Spec

**Date:** 2026-03-28
**Domain:** Ice Hockey Goalie Equipment Store (Bauer, CCM, True)
**Purpose:** POC / demo template for the Critterstack architecture (Marten + WolverineFX) on .NET Aspire

---

## 1. Goals

- Demonstrate full Critterstack architecture: Marten event sourcing per service + WolverineFX messaging
- Database-per-service isolation with a separate relational read model database
- Inter-service choreography via RabbitMQ (Wolverine transport + Marten transactional outbox)
- All infrastructure wired through .NET Aspire providers
- Working React frontend: browse items, place order, fake payment, view purchases

---

## 2. Technology Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| Orchestration | .NET Aspire 13.x |
| Event Store | Marten 7.x (per service, on PostgreSQL) |
| Messaging | WolverineFX 3.x |
| Message Transport | RabbitMQ (Wolverine.RabbitMQ) |
| Outbox | Wolverine + Marten transactional outbox |
| Read Model DB | PostgreSQL (EF Core, relational schema) |
| API Style | ASP.NET Core Minimal APIs |
| Frontend | React 18 + TypeScript + Vite |
| UI Components | shadcn/ui + Tailwind CSS |
| Data Fetching | TanStack Query |

---

## 3. Solution Structure

```
OrderDemo.slnx
│
├── src/
│   ├── AppHost/                        ← .NET Aspire orchestrator
│   ├── ServiceDefaults/                ← Shared Aspire observability/health defaults
│   ├── Contracts/                      ← Shared message types (events + commands, plain records only)
│   │
│   ├── Order/
│   │   └── Order.Api/                  ← ASP.NET Core minimal API + Wolverine + Marten
│   ├── Inventory/
│   │   └── Inventory.Api/              ← ASP.NET Core minimal API + Wolverine + Marten
│   ├── Payment/
│   │   └── Payment.Api/                ← ASP.NET Core minimal API + Wolverine + Marten
│   │
│   ├── ReadModel/
│   │   └── ReadModel.Projector/        ← Worker + HTTP query API, EF Core → readmodel-db
│   │
│   └── Frontend/
│       └── frontend/                   ← React + TypeScript + Vite
│
└── docs/
    └── superpowers/specs/
```

**Contracts project constraint:** plain C# records only — no Marten, no Wolverine, no EF Core dependencies.

---

## 4. Infrastructure & Port Map

### Aspire AppHost

```csharp
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("orderdemo-postgres-data")
    .WithPgAdmin(pgAdminPort: 5050);

var ordersDb    = postgres.AddDatabase("orders-db");
var inventoryDb = postgres.AddDatabase("inventory-db");
var paymentsDb  = postgres.AddDatabase("payments-db");
var readModelDb = postgres.AddDatabase("readmodel-db");

var rabbit = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithDataVolume("orderdemo-rabbitmq-data")
    .WithManagementPlugin(port: 15672);
```

### Port Reference

| Resource | Port | URL |
|----------|------|-----|
| PostgreSQL | 5432 | — |
| PgAdmin | 5050 | http://localhost:5050 |
| RabbitMQ AMQP | 5672 | — |
| RabbitMQ Management | 15672 | http://localhost:15672 |
| Inventory API | 7001 | https://localhost:7001 |
| Order API | 7002 | https://localhost:7002 |
| Payment API | 7003 | https://localhost:7003 |
| ReadModel Projector | 7004 | https://localhost:7004 |
| Frontend | 3000 | http://localhost:3000 |

### Persistence
- `orderdemo-postgres-data` — named Docker volume, survives restarts. Wipe with `docker volume rm orderdemo-postgres-data`
- `orderdemo-rabbitmq-data` — named Docker volume for durable RabbitMQ queues

---

## 5. Domain Model

### Aggregates

**InventoryItem** (Inventory.Api, Marten event-sourced)
```
id, brand, category, name, description, price, stockQuantity
```

**Order** (Order.Api, Marten event-sourced — saga root)
```
id, userId, items[], status, totalAmount, placedAt, confirmedAt, failureReason
```

**Payment** (Payment.Api, Marten event-sourced)
```
id, orderId, userId, amount, status, processedAt, failureReason
```

### Events (Contracts project — plain records)

```csharp
// Order domain
record OrderPlaced(Guid OrderId, Guid UserId, OrderItem[] Items, decimal Total);
record OrderConfirmed(Guid OrderId, DateTimeOffset ConfirmedAt);
record OrderFailed(Guid OrderId, string Reason);

// Inventory domain
record StockReserved(Guid OrderId, Guid[] ItemIds);
record StockReservationFailed(Guid OrderId, string Reason);
record StockReleased(Guid OrderId);          // compensating event

// Payment domain
record PaymentProcessed(Guid PaymentId, Guid OrderId, decimal Amount);
record PaymentFailed(Guid PaymentId, Guid OrderId, string Reason);

// Inventory item lifecycle
record InventoryItemCreated(Guid ItemId, string Brand, string Category,
    string Name, string Description, decimal Price, int InitialStock);
record StockQuantityUpdated(Guid ItemId, int NewQuantity);
```

### Supporting types (Contracts)
```csharp
record OrderItem(Guid ItemId, string ItemName, string Brand, int Quantity, decimal UnitPrice);
```

---

## 6. Choreography Flow

### Happy Path
```
1. POST /orders (Order.Api)
   → appends OrderPlaced to orders event stream
   → Wolverine outbox → RabbitMQ

2. Inventory.Api receives OrderPlaced
   → reserves stock for each item
   → appends StockReserved to inventory event stream
   → Wolverine outbox → RabbitMQ

3. Payment.Api receives StockReserved
   → processes fake payment
   → appends PaymentProcessed to payment event stream
   → Wolverine outbox → RabbitMQ

4. Order.Api receives PaymentProcessed
   → appends OrderConfirmed to orders event stream
   → Wolverine outbox → RabbitMQ

5. Projector receives all events → updates readmodel-db
```

### Failure Paths
```
StockReservationFailed →
  Order.Api: appends OrderFailed

PaymentFailed →
  Inventory.Api: appends StockReleased (compensation)
  Order.Api: appends OrderFailed
```

---

## 7. Per-Service Internal Structure

Each domain service follows the same layout:

```
Service.Api/
├── Program.cs
├── Domain/
│   └── [Aggregate].cs          ← Apply(event) methods only, no logic
├── Handlers/
│   ├── Commands/               ← Wolverine handlers triggered by HTTP endpoints
│   └── Events/                 ← Wolverine handlers triggered by incoming RabbitMQ events
├── Endpoints/                  ← Minimal API route registrations
└── Infrastructure/
    └── Seeding.cs              ← Inventory.Api only
```

### Wolverine + Marten wiring pattern (per service Program.cs)

```csharp
builder.Services.AddMarten(opts => {
    opts.Connection(builder.Configuration.GetConnectionString("orders-db")!);
}).IntegrateWithWolverine();

builder.Host.UseWolverine(opts => {
    opts.UseRabbitMqUsingNamedConnection("rabbitmq").AutoProvision();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.ForwardMartenEvents(typeof(OrderPlaced).Assembly);

    // Publish routing (example — Order.Api)
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderConfirmed>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderFailed>().ToRabbitExchange("order-events");

    // Subscribe routing (example — Order.Api)
    opts.ListenToRabbitQueue("order-api-inbox");
});
```

### Key wiring notes
- Do NOT call `builder.AddRabbitMQClient()` — Wolverine manages its own RabbitMQ connections
- `UseRabbitMqUsingNamedConnection("rabbitmq")` reads Aspire-injected `ConnectionStrings__rabbitmq`
- `IntegrateWithWolverine()` enables the transactional outbox and aggregate handler workflow
- `AutoApplyTransactions()` wraps every Wolverine handler in a Marten transaction automatically

---

## 8. Event Routing — RabbitMQ Exchanges & Queues

| Exchange | Published by | Subscribed by |
|----------|-------------|---------------|
| `order-events` | Order.Api | Inventory.Api, Projector |
| `inventory-events` | Inventory.Api | Payment.Api, Order.Api, Projector |
| `payment-events` | Payment.Api | Order.Api, **Inventory.Api**, Projector |

**Note:** Inventory.Api subscribes to `payment-events` to handle `PaymentFailed` and emit the compensating `StockReleased` event.

Each service has its own named inbox queue (e.g. `inventory-api-inbox`, `order-api-inbox`).
Projector has `projector-inbox` bound to all three exchanges.

---

## 9. ReadModel Schema (readmodel-db, EF Core)

```sql
CREATE TABLE inventory_items (
    id          UUID PRIMARY KEY,
    brand       TEXT NOT NULL,
    category    TEXT NOT NULL,
    name        TEXT NOT NULL,
    description TEXT,
    price       DECIMAL(10,2) NOT NULL,
    stock_qty   INTEGER NOT NULL
);

CREATE TABLE orders (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL,
    status          TEXT NOT NULL,       -- Pending | Confirmed | Failed
    total_amount    DECIMAL(10,2) NOT NULL,
    placed_at       TIMESTAMPTZ NOT NULL,
    confirmed_at    TIMESTAMPTZ,
    failure_reason  TEXT
);

CREATE TABLE order_lines (
    id          UUID PRIMARY KEY,
    order_id    UUID NOT NULL REFERENCES orders(id),
    item_id     UUID NOT NULL,
    item_name   TEXT NOT NULL,           -- denormalised at time of order
    brand       TEXT NOT NULL,
    quantity    INTEGER NOT NULL,
    unit_price  DECIMAL(10,2) NOT NULL
);
```

### Projector HTTP Query Endpoints

```
GET /api/catalog/items                      → all inventory items
GET /api/catalog/orders?userId={userId}     → all orders for user, newest first
GET /api/catalog/orders/{orderId}           → single order with line items
```

---

## 10. Seeding Data (Inventory.Api startup)

12 products seeded on first run if store is empty:

| Brand | Category | Name | Price | Stock |
|-------|----------|------|-------|-------|
| Bauer | Mask | Bauer Hyperlite2 Goalie Mask | $899.99 | 8 |
| CCM | Mask | CCM Axis 2.9 Goalie Mask | $749.99 | 10 |
| True | Mask | True Catalyst 9X2 Goalie Mask | $849.99 | 6 |
| Bauer | Pads | Bauer Hyperlite2 Leg Pads | $1,299.99 | 5 |
| CCM | Pads | CCM Axis A2.9 Leg Pads | $1,099.99 | 7 |
| True | Pads | True Catalyst 9X2 Leg Pads | $1,249.99 | 4 |
| Bauer | Glove & Blocker | Bauer Hyperlite2 Catch Glove | $449.99 | 12 |
| CCM | Glove & Blocker | CCM Axis 2.9 Blocker | $349.99 | 15 |
| True | Glove & Blocker | True Catalyst 9X2 Catch Glove | $399.99 | 9 |
| Bauer | Chest & Arms | Bauer Supreme Mach C/A | $599.99 | 6 |
| CCM | Chest & Arms | CCM Axis A2.9 C/A Protector | $549.99 | 8 |
| True | Chest & Arms | True Catalyst 9X2 C/A Protector | $579.99 | 5 |

Seeding fires `InventoryItemCreated` events through Marten — seeded items flow to the read model the same way real items would.

---

## 11. Frontend Design

**Stack:** React 18 + TypeScript + Vite + shadcn/ui + Tailwind CSS + TanStack Query

### User Identity (fake)
3 hardcoded users stored in `localStorage`. Nav includes a user switcher dropdown. No real auth.

```typescript
const USERS = [
  { id: "usr-001", name: "Mike Smith" },
  { id: "usr-002", name: "Sara Jones" },
  { id: "usr-003", name: "Dave Lee" },
]
```

### Pages

| Route | Page | Data Source |
|-------|------|-------------|
| `/` | Shop — product grid, filter by brand/category, cart | `GET /api/catalog/items` → Projector |
| `/checkout` | Cart review + fake payment form | `POST /api/orders` → Order.Api |
| `/orders` | My Purchases — order list with status badges | `GET /api/catalog/orders?userId=` → Projector |
| `/orders/:id` | Order Detail — line items, status, timestamps | `GET /api/catalog/orders/:id` → Projector |

### API Path Convention
- `/api/orders` (POST only) → Order.Api — write commands
- `/api/catalog/*` → Projector — all read queries (items + orders)

Vite can't differentiate proxy routes by HTTP method, so distinct path prefixes are required.

### API Proxy (vite.config.ts)
```typescript
server: {
  proxy: {
    '/api/orders':  'https://localhost:7002',   // write commands → Order.Api
    '/api/catalog': 'https://localhost:7004',   // read queries  → Projector
  }
}
```

### UX Notes
- My Purchases page auto-refreshes every 5 seconds — visible status transition Pending → Confirmed
- Order status badges: Pending (amber), Confirmed (green), Failed (red)
- Fake payment: any 16-digit card number accepted, no real validation

---

## 12. Key Architectural Constraints

1. **Contracts project** — records only, zero framework dependencies
2. **No `AddRabbitMQClient()`** — Wolverine owns the RabbitMQ connection
3. **Marten outbox is the write guarantee** — `SaveChangesAsync()` commits event + outbox entry atomically
4. **ReadModel is append/update only** — Projector never deletes, only upserts/updates
5. **Inventory seeding uses events** — not direct DB inserts, so the read model receives them naturally
6. **All infrastructure via Aspire** — no hardcoded connection strings anywhere in service code
