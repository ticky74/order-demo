// src/AppHost/AppHost.cs
var builder = DistributedApplication.CreateBuilder(args);

// ── PostgreSQL ─────────────────────────────────────────────
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("orderdemo-postgres-data")
    .WithPgAdmin(c => c.WithHostPort(5050));

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
    .WithNpm()
    .WithHttpEndpoint(port: 3000)
    .WithReference(inventoryApi)
    .WithReference(orderApi)
    .WithReference(projector)
    .WithExternalHttpEndpoints();

builder.Build().Run();
