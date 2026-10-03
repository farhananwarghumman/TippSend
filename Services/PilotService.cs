using System.Text.Json;
using TippSendApp.Models;
namespace TippSendApp.Services;
// Pilot store: one application process; separate from paid orders and never served as static content.
public class PilotService
{
    private readonly object _gate = new();
    private readonly string _path;
    public bool Preview { get; }
    public static DateTime IrishNow => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/Dublin");
    private class State { public List<PilotRoute> Routes { get; set; } = new(); public List<PilotRequest> Requests { get; set; } = new(); }
    public PilotService(IConfiguration config, IWebHostEnvironment env)
    {
        Preview = env.IsDevelopment() && config.GetValue<bool>("LocalPreview");
        _path = Path.Combine(config["PilotDataDir"] ?? config["AppSettingsDir"] ?? env.ContentRootPath, "App_Data", Preview ? "pilot-preview.json" : "pilot.json");
    }
    private State Read()
    {
        if (File.Exists(_path)) return JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) ?? throw new InvalidDataException("Invalid pilot store");
        var state = new State();
        if (Preview) {
            var day = IrishNow.Date.AddDays(1);
            while (day.DayOfWeek != DayOfWeek.Saturday) day = day.AddDays(1);
            state.Routes.Add(new PilotRoute { Id="preview-town", Corridor="Clonmel town", DepartureDate=day, Window="12:00–15:00", Cutoff=day.AddHours(10), Price=8 });
            state.Routes.Add(new PilotRoute { Id="preview-regional", Corridor="Clonmel → Cahir", DepartureDate=day, Window="15:00–18:00", Cutoff=day.AddHours(12), Price=16 });
        }
        return state;
    }
    private void Save(State state) {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented=true }));
        File.Move(temporary, _path, true);
    }
    public List<PilotRoute> Routes(bool availableOnly=true) { lock(_gate) { var s=Read(); return s.Routes.Where(r=>!availableOnly || r.Open && r.Cutoff>IrishNow && s.Requests.Count(x=>x.RouteId==r.Id && x.Status!="Cancelled")<r.Capacity).OrderBy(r=>r.DepartureDate).ToList(); } }
    public List<PilotRequest> Requests() { lock(_gate) return Read().Requests.OrderByDescending(r=>r.CreatedAt).ToList(); }
    public string Submit(PilotRequest request) {
        lock(_gate) {
            var s=Read();
            if(request.Service=="Scheduled") {
                var route=s.Routes.FirstOrDefault(r=>r.Id==request.RouteId);
                if(route is null || !route.Open || route.Cutoff<=IrishNow || s.Requests.Count(x=>x.RouteId==route.Id && x.Status!="Cancelled")>=route.Capacity) throw new InvalidOperationException("This departure is no longer available. Choose another run or request a dedicated quote.");
                request.PreferredDate=route.DepartureDate; request.PreferredTime=route.Window; request.QuotedPrice=route.Price;
            } else { request.RouteId=null; request.QuotedPrice=null; if(request.PreferredDate.Date<IrishNow.Date || request.PreferredDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) throw new InvalidOperationException("Choose a future Saturday or Sunday during the weekend pilot."); }
            request.Reference="TS-R-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(); request.Status="Requested"; request.CreatedAt=DateTime.UtcNow;
            s.Requests.Add(request); Save(s); return request.Reference;
        }
    }
    public void AddRoute(PilotRoute route) { lock(_gate) { if(route.Cutoff<=IrishNow || route.Cutoff>route.DepartureDate.AddDays(1) || route.DepartureDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) throw new InvalidOperationException("Publish a future weekend run with a valid cutoff."); var s=Read(); route.Id=Guid.NewGuid().ToString("N"); s.Routes.Add(route); Save(s); } }
    public void CloseRoute(string id) { lock(_gate) { var s=Read(); var r=s.Routes.FirstOrDefault(x=>x.Id==id); if(r is not null) r.Open=false; Save(s); } }
    public void UpdateRequest(string reference,string status,decimal? price) { lock(_gate) { if(!new[]{"Requested","Quoted","Accepted","Completed","Cancelled"}.Contains(status)) throw new InvalidOperationException("Invalid status."); if(price is <0 or >10000) throw new InvalidOperationException("Enter a valid quote."); var s=Read(); var r=s.Requests.Single(x=>x.Reference==reference); if(status is "Quoted" or "Accepted" && !(price>0)) throw new InvalidOperationException("A positive agreed price is required."); r.Status=status; r.QuotedPrice=price; Save(s); } }
}
