using Microsoft.AspNetCore.Identity.UI.Services;
namespace TippSendApp.Services;
public class IdentityEmailSender(EmailService email) : IEmailSender
{
    public Task SendEmailAsync(string address,string subject,string html) => email.QueueAsync(address,subject,html);
}
