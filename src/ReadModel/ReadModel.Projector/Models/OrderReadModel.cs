// src/ReadModel/ReadModel.Projector/Models/OrderReadModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadModel.Projector.Models;

[Table("orders")]
public class OrderReadModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? FailureReason { get; set; }

    public ICollection<OrderLineReadModel> Lines { get; set; } = [];
}
