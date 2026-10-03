using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TippSendApp.Data;
namespace TippSendApp.Pages;

[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class SetupModel(IConfiguration config, UserManager<IdentityUser> users, ApplicationDbContext db) : PageModel
{
    [BindProperty] public string Token {get;set;}="";
    [BindProperty,Required,MinLength(12),DataType(DataType.Password)] public string Password {get;set;}="";
    [BindProperty,Compare(nameof(Password)),DataType(DataType.Password)] public string ConfirmPassword {get;set;}="";
    public string Email => config["BootstrapAdmin:Email"] ?? "";
    private bool Valid(string token)
    {
        var expected=config["BootstrapAdmin:SetupToken"];
        return !string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(token) && !string.IsNullOrWhiteSpace(Email)
            && DateTimeOffset.TryParse(config["BootstrapAdmin:SetupExpiresAt"],out var expires) && expires>DateTimeOffset.UtcNow
            && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(token)),SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }
    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if(!Valid(token??"") || (await users.GetUsersInRoleAsync("Admin")).Any()) return NotFound();
        Token=token!; return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if(!Valid(Token))return NotFound();
        if(!ModelState.IsValid)return Page();
        await using var transaction=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(912045)");
        if((await users.GetUsersInRoleAsync("Admin")).Any())return NotFound();
        if(await users.FindByEmailAsync(Email) is not null) { ModelState.AddModelError("","This email is already registered. Contact the site administrator.");return Page(); }
        var user=new IdentityUser{Email=Email,UserName=Email,EmailConfirmed=true};
        var created=await users.CreateAsync(user,Password);
        if(!created.Succeeded){foreach(var e in created.Errors)ModelState.AddModelError("",e.Description);return Page();}
        var role=await users.AddToRoleAsync(user,"Admin");
        if(!role.Succeeded)throw new InvalidOperationException("Admin role could not be assigned.");
        await transaction.CommitAsync();
        return Redirect("/Identity/Account/Login?returnUrl=%2FAdmin");
    }
}
