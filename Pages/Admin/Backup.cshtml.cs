using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TippSendApp.Services;
namespace TippSendApp.Pages.Admin;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class BackupModel(BackupService backup):PageModel
{
    public void OnGet(){}
    public async Task<IActionResult> OnPostDownloadAsync()=>File(await backup.ExportAsync(),"application/zip",$"tippsend-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
}
