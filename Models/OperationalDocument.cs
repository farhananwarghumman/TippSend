using System.ComponentModel.DataAnnotations;
namespace TippSendApp.Models;

// Transactional operational records, retained independently of application restarts.
public class OperationalDocument
{
    [Key, MaxLength(100)] public string Key { get; set; } = "";
    public string Json { get; set; } = "{}";
}

public class PaymentDraft
{
    [Key, MaxLength(64)] public string Token { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Json { get; set; } = "{}";
    public long AmountCents { get; set; }
    public string? StripeSessionId { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Html { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public string? LastError { get; set; }
}
