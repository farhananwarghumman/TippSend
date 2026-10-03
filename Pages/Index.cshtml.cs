using Microsoft.AspNetCore.Mvc.RazorPages;
using TippSendApp.Services;
using TippSendApp.Models;
namespace TippSendApp.Pages;
public class IndexModel : PageModel {
 private readonly PilotService _pilot;
 public IndexModel(PilotService pilot)=>_pilot=pilot;
 public List<PilotRoute> Routes { get; private set; }=new();
 public bool Preview=>_pilot.Preview;
 public void OnGet()=>Routes=_pilot.Routes();
}
