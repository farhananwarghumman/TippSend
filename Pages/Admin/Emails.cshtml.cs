using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TippSendApp.Data;
using TippSendApp.Models;
namespace TippSendApp.Pages.Admin;
public class EmailsModel(ApplicationDbContext db,IConfiguration config):PageModel
{
    public bool Configured => !string.IsNullOrWhiteSpace(config["Resend:ApiKey"]);
    public List<EmailMessage> Messages {get;private set;}=new();
    public async Task OnGetAsync()=>Messages=await db.EmailMessages.OrderByDescending(m=>m.CreatedAt).Take(100).ToListAsync();
    public async Task<IActionResult> OnPostRetryAsync(Guid id)
    {
        var message=await db.EmailMessages.FindAsync(id);
        if(message is not null && message.SentAt is null){message.Attempts=0;message.NextAttemptAt=DateTime.UtcNow;await db.SaveChangesAsync();}
        return RedirectToPage();
    }
}
