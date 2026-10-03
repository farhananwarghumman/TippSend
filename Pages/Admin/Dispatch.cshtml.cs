using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TippSendApp.Models;
using TippSendApp.Services;
namespace TippSendApp.Pages.Admin;
public class DispatchModel : PageModel {
    private readonly PilotService _pilot;
    public DispatchModel(PilotService pilot)=>_pilot=pilot;
    [BindProperty] public PilotRoute NewRoute { get; set; } = new();
    public List<PilotRoute> Routes { get; set; }=new();
    public List<PilotRequest> Requests { get; set; }=new();
    public void OnGet()=>Load();
    private void Load(){Routes=_pilot.Routes(false);Requests=_pilot.Requests();}
    public IActionResult OnPostPublish(){ if(ModelState.IsValid)try{_pilot.AddRoute(NewRoute);return RedirectToPage();}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}Load();return Page(); }
    public IActionResult OnPostClose(string id){_pilot.CloseRoute(id);return RedirectToPage();}
    public IActionResult OnPostUpdate(string reference,string status,decimal? price){try{_pilot.UpdateRequest(reference,status,price);return RedirectToPage();}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);Load();return Page();}}
}
