// src/Payment/Payment.Api/Program.cs
using Contracts.Events;
using Payment.Api.Endpoints;
using Marten;
using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("payments-db")!);
    opts.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
})
.IntegrateWithWolverine()
.ApplyAllDatabaseChangesOnStartup();

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("rabbitmq")
        .AutoProvision()
        .BindExchange("inventory-events").ToQueue("payment-api-inbox");

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

    opts.PublishMessage<PaymentProcessed>().ToRabbitExchange("payment-events");
    opts.PublishMessage<PaymentFailed>().ToRabbitExchange("payment-events");

    opts.ListenToRabbitQueue("payment-api-inbox");
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.MapDefaultEndpoints();
app.MapPaymentEndpoints();
app.Run();
