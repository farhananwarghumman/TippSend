using Microsoft.AspNetCore.Mvc.RazorPages;
using TippSendApp.Services;
namespace TippSendApp.Pages.Admin;
public class EnquiriesModel(MerchantEnquiryService service):PageModel
{
    public List<MerchantEnquiry> Enquiries {get;private set;}=new();
    public void OnGet()=>Enquiries=service.List();
}
