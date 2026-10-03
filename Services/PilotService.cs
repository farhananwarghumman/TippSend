using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using TippSendApp.Models;
namespace TippSendApp.Services;

public class PilotService
{
    private readonly object _gate = new();
    private readonly string _path;
    private readonly OperationalStore? _store;
    public bool Preview { get; }
    public static DateTime IrishNow => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/Dublin");
    public class State { public List<PilotRoute> Routes { get; set; } = new(); public List<PilotRequest> Requests { get; set; } = new(); }
    public PilotService(IConfiguration config, IWebHostEnvironment env, OperationalStore? store = null)
    {
        Preview = env.IsDevelopment() && config.GetValue<bool>("LocalPreview");
        _store = Preview ? null : store;
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
    private T Execute<T>(Func<State,T> operation, bool write = false)
    {
        if (_store is not null) return _store.Execute("pilot", Read, operation, write);
        lock (_gate) {
            var state = Read(); var result = operation(state);
            if (write) {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(state));
                File.Move(_path + ".tmp", _path, true);
            }
            return result;
        }
    }
    public List<PilotRoute> Routes(bool availableOnly=true) => Execute(s => s.Routes.Where(r=>!availableOnly || r.Open && r.Cutoff>IrishNow && s.Requests.Count(x=>x.RouteId==r.Id && x.Status!="Cancelled")<r.Capacity).OrderBy(r=>r.DepartureDate).ToList());
    public List<PilotRequest> Requests() => Execute(s=>s.Requests.OrderByDescending(r=>r.CreatedAt).ToList());
    public PilotRequest? Find(string token) => Execute(s=>s.Requests.SingleOrDefault(r=>r.TrackingToken==token));
    public string Submit(PilotRequest request) => Execute(s => {
        if(request.Service=="Scheduled" && string.IsNullOrWhiteSpace(request.PreferredTime)) request.PreferredTime="Confirmed run window";
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if(request.Service=="Scheduled") {
            var route=s.Routes.FirstOrDefault(r=>r.Id==request.RouteId);
            if(route is null || !route.Open || route.Cutoff<=IrishNow || s.Requests.Count(x=>x.RouteId==route.Id && x.Status!="Cancelled")>=route.Capacity) throw new InvalidOperationException("This departure is no longer available. Choose another run or request a dedicated quote.");
            request.PreferredDate=route.DepartureDate; request.PreferredTime=route.Window; request.QuotedPrice=route.Price;
        } else {
            request.RouteId=null; request.QuotedPrice=null;
            if(request.PreferredDate.Date<IrishNow.Date || request.PreferredDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) throw new InvalidOperationException("Choose a future Saturday or Sunday during the weekend pilot.");
        }
        request.Reference="TS-R-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        request.TrackingToken=Guid.NewGuid().ToString("N"); request.Status="Requested"; request.CreatedAt=DateTime.UtcNow;
        s.Requests.Add(request); return request.Reference;
    }, true);
    public void AddRoute(PilotRoute route) => Execute(s => {
        Validator.ValidateObject(route, new ValidationContext(route), true);
        if(route.Cutoff<=IrishNow || route.Cutoff>route.DepartureDate.AddDays(1) || route.DepartureDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) throw new InvalidOperationException("Publish a future weekend run with a valid cutoff.");
        route.Id=Guid.NewGuid().ToString("N"); s.Routes.Add(route); return true;
    }, true);
    public void CloseRoute(string id) => Execute(s=> { var route=s.Routes.FirstOrDefault(x=>x.Id==id); if(route is not null) route.Open=false; return true; },true);
    public void UpdateRequest(string reference,string status,decimal? price) => Execute(s=> {
        if(!new[]{"Requested","Quoted","Accepted","Completed","Cancelled"}.Contains(status)) throw new InvalidOperationException("Invalid status.");
        if(price is <0 or >10000) throw new InvalidOperationException("Enter a valid quote.");
        var r=s.Requests.SingleOrDefault(x=>x.Reference==reference) ?? throw new InvalidOperationException("Request not found.");
        if(status is "Quoted" or "Accepted" && !(price>0)) throw new InvalidOperationException("A positive agreed price is required.");
        if(r.Status=="Completed" && status!="Completed") throw new InvalidOperationException("A completed delivery cannot be reopened here.");
        r.Status=status; r.QuotedPrice=price; return true;
    },true);
}
