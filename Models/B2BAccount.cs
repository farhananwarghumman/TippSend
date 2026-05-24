using System.ComponentModel.DataAnnotations;

namespace TippSendApp.Models;

public class B2BAccount
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string BusinessName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ContactName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(30)]
    public string ContactPhone { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Town { get; set; } = string.Empty;

    public decimal MonthlyRetainer { get; set; }
    public decimal PerDeliveryRate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
