using System.Text.Json;
using TippSendApp.Models;
namespace TippSendApp.Services;
public class MerchantEnquiry
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string Business {get;set;}="";
    public string Type {get;set;}="";
    public string Volume {get;set;}="";
    public string Name {get;set;}="";
    public string Phone {get;set;}="";
    public string Email {get;set;}="";
    public string Needs {get;set;}="";
    public DateTime CreatedAt {get;set;}=DateTime.UtcNow;
}
public class MerchantEnquiryService
{
    private readonly OperationalStore _store;
    private readonly string _dir;
    public MerchantEnquiryService(IConfiguration c,IWebHostEnvironment e,PilotService p,OperationalStore store)
    { _store=store; _dir=Path.Combine(c["PilotDataDir"]??c["AppSettingsDir"]??e.ContentRootPath,"App_Data",p.Preview?"merchant-preview":"merchant-enquiries"); }
    private List<MerchantEnquiry> Initial()
    {
        var result=new List<MerchantEnquiry>();
        if(!Directory.Exists(_dir))return result;
        foreach(var file in Directory.EnumerateFiles(_dir,"*.json")) {
            using var doc=JsonDocument.Parse(File.ReadAllText(file)); var x=doc.RootElement;
            string V(string key)=>x.TryGetProperty(key,out var value)?value.GetString()??"":"";
            result.Add(new MerchantEnquiry {Business=V("business"),Type=V("type"),Volume=V("volume"),Name=V("name"),Phone=V("phone"),Email=V("email"),Needs=V("needs"),CreatedAt=x.GetProperty("createdAt").GetDateTime()});
        }
        return result;
    }
    public List<MerchantEnquiry> List()=>_store.Execute("merchant-enquiries",Initial,s=>s.OrderByDescending(x=>x.CreatedAt).ToList());
    public void Save(string business,string type,string volume,string name,string phone,string email,string needs)
        =>_store.Execute("merchant-enquiries",Initial,s=>{s.Add(new MerchantEnquiry{Business=business,Type=type,Volume=volume,Name=name,Phone=phone,Email=email,Needs=needs});return true;},true);
}
