using System.ComponentModel.DataAnnotations;
using System.Net;
using TippSendApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TippSendApp.Pages.Partner;

public class IndexModel : PageModel
{
    private readonly EmailService _email;
    public IndexModel(EmailService email) => _email = email;

    [BindProperty, Required(ErrorMessage = "Please enter your business name.")]
    public string BusinessName { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please select your shop type.")]
    public string ShopType { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please select estimated weekly deliveries.")]
    public string WeeklyDeliveries { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your name.")]
    public string ContactName { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your phone number.")]
    public string Phone { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Please enter your email address."), EmailAddress]
    public string Email { get; set; } = "";

    public bool Submitted { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        static string E(string s) => WebUtility.HtmlEncode(s);

        var html = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:sans-serif;background:#F6F0E2;margin:0;padding:40px 0">
            <div style="max-width:520px;margin:0 auto;background:#FAF6EC;border-radius:12px;overflow:hidden">
              <div style="background:#1F3A2E;padding:24px;text-align:center">
                <div style="font-size:22px;color:#C9A24B;font-family:Georgia,serif;font-style:italic">TippSend</div>
                <div style="color:rgba(246,240,226,0.6);font-size:11px;margin-top:4px;letter-spacing:0.1em;text-transform:uppercase">New B2B Partner Enquiry</div>
              </div>
              <div style="padding:32px">
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363;width:44%">Business name</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{E(BusinessName)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Shop type</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{E(ShopType)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Weekly deliveries</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{E(WeeklyDeliveries)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Contact name</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{E(ContactName)}</td></tr>
                  <tr><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;color:#7A7363">Phone</td><td style="padding:9px 0;border-bottom:1px solid #DDD4BA;text-align:right">{E(Phone)}</td></tr>
                  <tr><td style="padding:9px 0;color:#7A7363">Email</td><td style="padding:9px 0;text-align:right">{E(Email)}</td></tr>
                </table>
              </div>
            </div>
            </body>
            </html>
            """;

        await _email.SendEnquiryAsync($"New B2B Enquiry — {BusinessName}", html);
        Submitted = true;
        return Page();
    }
}
