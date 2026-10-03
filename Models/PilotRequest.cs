using System.ComponentModel.DataAnnotations;
namespace TippSendApp.Models;
public class PilotRequest
{
    public string Reference { get; set; } = "";
    public string TrackingToken { get; set; } = Guid.NewGuid().ToString("N");
    [Display(Name="Delivery service"), Required(ErrorMessage="Choose a delivery service."), RegularExpression("^(Scheduled|Dedicated)$", ErrorMessage="Choose scheduled or separate delivery.")] public string Service { get; set; } = "Dedicated";
    public string? RouteId { get; set; }
    [Display(Name="Pickup address"), Required(ErrorMessage="Enter the full pickup address."), StringLength(300)] public string PickupAddress { get; set; } = "";
    [Display(Name="Pickup Eircode"), Required(ErrorMessage="Enter the pickup Eircode."), RegularExpression(@"^[A-Za-z0-9]{3}\s?[A-Za-z0-9]{4}$", ErrorMessage="Enter a seven-character pickup Eircode, such as E91 A123.")] public string PickupEircode { get; set; } = "";
    [Display(Name="Delivery address"), Required(ErrorMessage="Enter the full delivery address."), StringLength(300)] public string DropoffAddress { get; set; } = "";
    [Display(Name="Delivery Eircode"), Required(ErrorMessage="Enter the delivery Eircode."), RegularExpression(@"^[A-Za-z0-9]{3}\s?[A-Za-z0-9]{4}$", ErrorMessage="Enter a seven-character delivery Eircode, such as E91 A123.")] public string DropoffEircode { get; set; } = "";
    [Display(Name="Item description"), Required(ErrorMessage="Describe the item you want to send."), StringLength(500)] public string ItemDescription { get; set; } = "";
    [Display(Name="Item weight"), Range(0.1,25, ErrorMessage="Enter a weight between 0.1 and 25 kg.")] public decimal WeightKg { get; set; } = 1;
    [Display(Name="Item value"), Range(0,500, ErrorMessage="Enter an item value between €0 and €500.")] public decimal DeclaredValue { get; set; }
    [Display(Name="Your name"), Required(ErrorMessage="Enter your name."), StringLength(100)] public string ContactName { get; set; } = "";
    [Display(Name="Your email"), Required(ErrorMessage="Enter your email address."), EmailAddress(ErrorMessage="Enter a valid email address."), StringLength(200)] public string ContactEmail { get; set; } = "";
    [Display(Name="Your phone number"), Required(ErrorMessage="Enter your phone number."), Phone(ErrorMessage="Enter a valid phone number."), StringLength(30)] public string ContactPhone { get; set; } = "";
    [Display(Name="Recipient name"), Required(ErrorMessage="Enter the name of the person receiving the item."), StringLength(100)] public string RecipientName { get; set; } = "";
    [Display(Name="Recipient phone"), Required(ErrorMessage="Enter the recipient’s phone number."), Phone(ErrorMessage="Enter a valid recipient phone number."), StringLength(30)] public string RecipientPhone { get; set; } = "";
    [Display(Name="Preferred date")] public DateTime PreferredDate { get; set; }
    [Display(Name="Preferred time"), Required(ErrorMessage="Enter a preferred time, or tell us you are flexible."), StringLength(100)] public string PreferredTime { get; set; } = "Flexible within the agreed window";
    [Display(Name="Access or handling notes"), StringLength(500)] public string? SpecialInstructions { get; set; }
    [Range(typeof(bool), "true", "true", ErrorMessage="Confirm the item is suitably packaged, eligible and fits in a standard car boot.")] public bool Eligible { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Requested";
    public decimal? QuotedPrice { get; set; }
}
public class PilotRoute
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [Required, StringLength(100)] public string Corridor { get; set; } = "";
    public DateTime DepartureDate { get; set; }
    [Required, StringLength(100)] public string Window { get; set; } = "";
    public DateTime Cutoff { get; set; }
    [Range(1,50)] public int Capacity { get; set; } = 9;
    [Range(1,1000)] public decimal Price { get; set; } = 16;
    public bool Open { get; set; } = true;
    public string Label => $"{Corridor} · {DepartureDate:ddd d MMM} · {Window} · €{Price:F2}";
}
