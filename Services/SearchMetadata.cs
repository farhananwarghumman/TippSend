using System.Text.Json;
using System.Xml.Linq;

namespace TippSendApp.Services;

// Only public informational pages belong in search. Never derive URLs from query
// strings (request references, checkout sessions and tracking tokens are private).
public static class SearchMetadata
{
    public const string Origin = "https://www.tippsend.ie";
    public record Page(string Path, string Title, string Description);
    public static readonly Page[] Pages =
    [
        new("/", "Local delivery in South Tipperary", "Collection and delivery for homes and businesses in Clonmel, Cahir, Cashel and Tipperary Town. Request a delivery with price and availability agreed upfront."),
        new("/send", "Send an item in South Tipperary", "Request collection from a home or business and delivery to another address in South Tipperary. We confirm availability and the full price before you pay."),
        new("/partner", "Business delivery in South Tipperary", "Arrange local delivery of prepared customer orders in South Tipperary. Ask TippSend about scheduled runs, separate journeys and pay-per-delivery business service."),
        new("/support", "Delivery questions and support", "Find answers about TippSend delivery areas, weekend availability, quotes, suitable items and home-to-home deliveries. Contact contact@tippsend.ie for help."),
        new("/Privacy", "Privacy policy", "Read how TippSend handles your contact details and delivery information when you request or use our local delivery service."),
        new("/terms", "Delivery terms and conditions", "Read TippSend delivery terms, including requests, acceptance, pricing, item restrictions and cancellation arrangements.")
    ];

    public static Page? Find(string? path) => Pages.FirstOrDefault(page =>
        string.Equals(page.Path, string.IsNullOrEmpty(path) || path == "/" ? "/" : path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

    public static string Sitemap()
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        return new XDocument(new XElement(ns + "urlset", Pages.Select(page =>
            new XElement(ns + "url", new XElement(ns + "loc", Origin + page.Path))))).ToString();
    }

    public static string OrganizationJson() => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Organization",
        ["@id"] = Origin + "/#organization",
        ["name"] = "TippSend",
        ["url"] = Origin + "/",
        ["email"] = "contact@tippsend.ie",
        ["telephone"] = "+353899707022",
        ["description"] = "Local collection and delivery for businesses and everyday items in South Tipperary, Ireland. Availability and the full price are agreed before a delivery is confirmed.",
        ["areaServed"] = new[] { "Clonmel", "Cahir", "Cashel", "Tipperary Town" },
        ["contactPoint"] = new { @type = "ContactPoint", email = "contact@tippsend.ie", telephone = "+353899707022", contactType = "customer support", availableLanguage = "English" }
    });
}
