// src/Order/Order.Api/Program.cs
using Contracts.Events;
using Order.Api.Endpoints;
using Marten;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// ── Marten ──────────────────────────────────────────────────
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("orders-db")!);
    opts.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;   // Marten 8.x namespace
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

// ── Wolverine ────────────────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .BindExchange("inventory-events").ToQueue("order-api-inbox")
        .BindExchange("payment-events").ToQueue("order-api-inbox");

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    // Publish order events to the order-events exchange
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderConfirmed>().ToRabbitExchange("order-events");
    opts.PublishMessage<OrderFailed>().ToRabbitExchange("order-events");

    // Listen on the inbox queue
    opts.ListenToRabbitQueue("order-api-inbox");
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.MapDefaultEndpoints();
app.MapOrderEndpoints();
app.Run();
