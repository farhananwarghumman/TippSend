using System.ComponentModel.DataAnnotations;

namespace TippSendApp.Models;

public class Shop
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Town { get; set; } = string.Empty;

    [MaxLength(100)]
    public string OpeningHours { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [MaxLength(300)]
    public string? PaymentNotes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
