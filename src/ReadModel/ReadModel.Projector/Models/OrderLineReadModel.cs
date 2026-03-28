// src/ReadModel/ReadModel.Projector/Models/OrderLineReadModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadModel.Projector.Models;

[Table("order_lines")]
public class OrderLineReadModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public OrderReadModel Order { get; set; } = null!;

    public Guid ItemId { get; set; }

    [Required, MaxLength(200)]
    public string ItemName { get; set; } = "";

    [Required, MaxLength(100)]
    public string Brand { get; set; } = "";

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
}
