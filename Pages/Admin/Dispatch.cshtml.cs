using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TippSendApp.Models;
using TippSendApp.Services;
namespace TippSendApp.Pages.Admin;
public class DispatchModel : PageModel {
    private readonly PilotService _pilot;
    private readonly TippSendApp.Data.ApplicationDbContext _db;
    private readonly EmailService _email;
    public DispatchModel(PilotService pilot, TippSendApp.Data.ApplicationDbContext db, EmailService email){_pilot=pilot;_db=db;_email=email;}
    [BindProperty] public PilotRoute NewRoute { get; set; } = new();
    public List<PilotRoute> Routes { get; set; }=new();
    public List<PilotRequest> Requests { get; set; }=new();
    public Dictionary<string,Order> PaidOrders {get;set;}=new();
    public void OnGet()=>Load();
    private void Load(){Routes=_pilot.Routes(false);Requests=_pilot.Requests();PaidOrders=_db.Orders.Where(o=>o.OrderType==OrderType.PickupDrop).ToDictionary(o=>o.TrackingToken);}
    public IActionResult OnPostPublish(){ if(ModelState.IsValid)try{_pilot.AddRoute(NewRoute);return RedirectToPage();}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);}Load();return Page(); }
    public IActionResult OnPostClose(string id){_pilot.CloseRoute(id);return RedirectToPage();}
    public async Task<IActionResult> OnPostUpdateAsync(string reference,string status,decimal? price){try{
        var request=_pilot.Requests().SingleOrDefault(r=>r.Reference==reference) ?? throw new InvalidOperationException("Request not found.");
        if(_db.PaymentDrafts.Any(p=>p.Token==request.TrackingToken)) throw new InvalidOperationException("A checkout has already started. Manage the paid order or contact the customer before changing this quote.");
        _pilot.UpdateRequest(reference,status,price);
        var updated=_pilot.Find(request.TrackingToken)!;
        if(request.Status!=updated.Status || request.QuotedPrice!=updated.QuotedPrice) await _email.SendRequestUpdateAsync(updated);
        return RedirectToPage();
    }catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);Load();return Page();}}
}
