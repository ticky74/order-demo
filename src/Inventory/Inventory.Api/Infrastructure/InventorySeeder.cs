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
