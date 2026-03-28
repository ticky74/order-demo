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
