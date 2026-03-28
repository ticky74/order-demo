# OrderDemo — Critterstack POC

## Project Goal

Build a proof-of-concept microservice application using the **Critterstack** architecture (Marten + WolverineFX) as an order system demo. This is a greenfield project.

## Current State

- **Phase:** Implementation underway — Task 1 (solution scaffolding) complete
- Solution structure scaffolded: `.slnx` solution, `AppHost`, `Contracts`, `Inventory.Api`, `Order.Api`, `Payment.Api`, and `ReadModel.Projector` projects all created and added to solution
- `.gitignore` in place; build artifacts excluded from source control

## Requirements (as stated)

### Backend
- **.NET 10** throughout
- **.NET Aspire** for orchestration — use .NET Aspire providers
- **PostgreSQL** for all databases
- **Database-per-service** approach (each service has its own isolated Postgres DB)
- **WolverineFX** for inter-service message handling
- **Marten** (implied by Critterstack) for event sourcing / document store on top of Postgres
- **Relational read model** in a completely separate Postgres database (cross-service projections/queries)

### Services
1. **Order Service** — creates and manages orders
2. **Inventory Service** — manages stock/items available to order
3. **Payment Service** — processes (fake) payments

### Frontend
- Technology: TBD — user said "use your best judgement"
- Must support:
  - Browse/select an item
  - Place an order
  - Take a (fake) payment
  - View current purchases

## Outstanding Design Questions (brainstorming in progress)

The following have NOT been answered yet — pick up from here:

1. **Wolverine transport** — In-process (Wolverine's local queues), or external broker (RabbitMQ, Azure Service Bus)? The POC nature suggests in-process or Wolverine's built-in TCP transport to keep infra minimal.
2. **Event sourcing depth** — Full event sourcing with Marten event store per service, or Marten as a document store only (simpler)?
3. **Read model mechanism** — Marten async projections pushing into a separate read DB, or a dedicated projection service consuming Wolverine messages?
4. **Frontend framework** — Blazor (stays in .NET ecosystem), React/Next.js, or Vue? Blazor is natural given the .NET-heavy stack.
5. **Auth** — Any authentication needed, or open/anonymous for the POC?

## Architecture Sketch (provisional, pending design approval)

```
┌─────────────────────────────────────────────────────┐
│                  .NET Aspire Host                    │
│                                                      │
│  ┌──────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ Order Service│  │  Inventory  │  │   Payment   │ │
│  │  (Marten +   │  │  Service    │  │   Service   │ │
│  │  Wolverine)  │  │  (Marten +  │  │  (Marten +  │ │
│  │  own PG DB   │  │  Wolverine) │  │  Wolverine) │ │
│  │              │  │  own PG DB  │  │  own PG DB  │ │
│  └──────┬───────┘  └──────┬──────┘  └──────┬──────┘ │
│         │                 │                │         │
│         └─────────────────┼────────────────┘         │
│                    Wolverine Messages                 │
│                           │                          │
│              ┌────────────▼────────────┐             │
│              │  Read Model Projector   │             │
│              │  (separate PG DB)       │             │
│              └─────────────────────────┘             │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │           Frontend (TBD)                        │ │
│  └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## Brainstorming Progress

Following the `superpowers:brainstorming` skill checklist:

- [x] Explore project context
- [ ] Offer visual companion — **PAUSED HERE** (user said "hang on", installing plugins)
- [ ] Ask clarifying questions (one at a time)
- [ ] Propose 2-3 approaches with trade-offs
- [ ] Present design sections
- [ ] Write design doc → `docs/superpowers/specs/`
- [ ] Spec self-review
- [ ] User reviews spec
- [ ] Invoke `writing-plans` skill

## Key Constraints

- This is a **POC / demo template** — simplicity and clarity of architecture pattern matter more than production hardening
- The Critterstack pattern (Marten + Wolverine) is a **hard requirement**, not up for debate
- All infrastructure should be wired through **.NET Aspire providers** (no manual connection strings)
