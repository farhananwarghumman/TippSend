namespace TippSendApp.Models;

public class PickupDropBooking
{
    public string PickupAddress { get; set; } = "";
    public string PickupZone { get; set; } = "";
    public string DropoffAddress { get; set; } = "";
    public string DropoffZone { get; set; } = "";
    public string ItemDescription { get; set; } = "";
    public string PreferredDate { get; set; } = "";
    public string PreferredTime { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string SpecialInstructions { get; set; } = "";
    public decimal Price { get; set; }
}
