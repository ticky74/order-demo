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
        .BindExchange("order-events").ToQueue("projector-inbox")
        .BindExchange("inventory-events").ToQueue("projector-inbox")
        .BindExchange("payment-events").ToQueue("projector-inbox");

    // Projector does not publish messages — only listens
    opts.ListenToRabbitQueue("projector-inbox");
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

app.UseCors();
app.MapDefaultEndpoints();
app.MapCatalogEndpoints();
app.Run();
