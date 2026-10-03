using System.Text.Json;
namespace TippSendApp.Services;
public class MerchantEnquiryService {
    private readonly object _gate=new(); private readonly string _dir;
    public MerchantEnquiryService(IConfiguration c,IWebHostEnvironment e,PilotService p) { _dir=Path.Combine(c["PilotDataDir"]??c["AppSettingsDir"]??e.ContentRootPath,"App_Data",p.Preview?"merchant-preview":"merchant-enquiries"); }
    public void Save(string business,string type,string volume,string name,string phone,string email,string needs) { lock(_gate) { Directory.CreateDirectory(_dir); File.WriteAllText(Path.Combine(_dir,Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(new{business,type,volume,name,phone,email,needs,createdAt=DateTime.UtcNow})); } }
}
