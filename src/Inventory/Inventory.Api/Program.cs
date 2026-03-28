// src/Inventory/Inventory.Api/Program.cs
using Contracts.Events;
using Inventory.Api.Endpoints;
using Inventory.Api.Infrastructure;
using Marten;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── Marten (event store) ────────────────────────────────────
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("inventory-db")!);
    opts.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

// ── Wolverine (messaging) ────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        // Bind incoming exchanges to the inventory inbox queue
        .BindExchange("order-events").ToQueue("inventory-api-inbox")
        .BindExchange("payment-events").ToQueue("inventory-api-inbox");

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Publish inventory events to the inventory-events exchange
    opts.PublishMessage<InventoryItemCreated>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReserved>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReservationFailed>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockReleased>().ToRabbitExchange("inventory-events");
    opts.PublishMessage<StockQuantityUpdated>().ToRabbitExchange("inventory-events");

    // Listen on the inventory inbox queue (bound to order-events and payment-events above)
    opts.ListenToRabbitQueue("inventory-api-inbox");
});

builder.Services.AddHostedService<InventorySeeder>();

// ── CORS (allow frontend dev server) ────────────────────────
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.MapDefaultEndpoints();
app.MapInventoryEndpoints();
app.Run();
