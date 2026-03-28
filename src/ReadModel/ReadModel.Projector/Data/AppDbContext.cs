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
