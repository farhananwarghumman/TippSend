using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using TippSendApp.Data;
using TippSendApp.Models;
using TippSendApp.Services;
using Stripe.Checkout;
using System.Collections.Concurrent;
using System.IO.Compression;
var connection=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")??throw new Exception("Disposable test database required.");
var cs=new Npgsql.NpgsqlConnectionStringBuilder(connection);
if(cs.Host!="localhost" || cs.Database!="tippsend_test")throw new Exception("Refusing to run against a non-test database.");
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
 ["Stripe:SecretKey"]="sk_test_disposable",["Stripe:WebhookSecret"]="whsec_disposable",["PublicBaseUrl"]="https://example.invalid"
}).Build();
var collection=new ServiceCollection();collection.AddSingleton<IConfiguration>(config);collection.AddSingleton<IWebHostEnvironment>(new TestEnvironment());
collection.AddLogging();collection.AddHttpClient();collection.AddDbContextFactory<ApplicationDbContext>(o=>o.UseNpgsql(connection));
collection.AddSingleton<OperationalStore>();collection.AddSingleton<PilotService>();collection.AddSingleton<AppSettingsService>();
collection.AddSingleton<MerchantEnquiryService>();collection.AddScoped<PricingService>();collection.AddScoped<EmailService>();
collection.AddScoped<WhatsAppService>();collection.AddScoped<OrderService>();collection.AddScoped<StripeService>();collection.AddScoped<PaymentService>();collection.AddScoped<BackupService>();
using var services=collection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes=true,ValidateOnBuild=true});
using(var scope=services.CreateScope())await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
var pilot=services.GetRequiredService<PilotService>();
var day=PilotService.IrishNow.Date.AddDays(1);while(day.DayOfWeek!=DayOfWeek.Saturday)day=day.AddDays(1);
PilotRequest Request()=>new(){Service="Scheduled",PickupAddress="Test pickup",PickupEircode="E91 A123",DropoffAddress="Test destination",DropoffEircode="E91 B123",ItemDescription="Test parcel",ContactName="Test sender",ContactEmail="sender@example.invalid",ContactPhone="0871234567",RecipientName="Test recipient",RecipientPhone="0877654321",Eligible=true,PreferredDate=day};
var route=new PilotRoute{Corridor="Test only",DepartureDate=day,Cutoff=day.AddHours(10),Window="12:00–15:00",Capacity=1,Price=18};pilot.AddRoute(route);
var results=new ConcurrentBag<string>();
Parallel.For(0,8,_=>{var separate=new PilotService(config,new TestEnvironment(),new OperationalStore(services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>()));var request=Request();request.RouteId=route.Id;try{results.Add(separate.Submit(request));}catch(InvalidOperationException){}});
Check(results.Count==1,"Concurrent route capacity exceeded");
var saved=pilot.Requests().Single(r=>r.Reference==results.Single());
Check(saved.QuotedPrice==18&&pilot.Find(saved.TrackingToken)?.Reference==saved.Reference,"Request persistence failed");
pilot.UpdateRequest(saved.Reference,"Accepted",18);
var merchant=services.GetRequiredService<MerchantEnquiryService>();merchant.Save("Test business","Shop","5","Test","0871234567","business@example.invalid","Test requirements");Check(merchant.List().Any(x=>x.Business=="Test business"),"Enquiry not stored");
PaymentDraft draft;
using(var scope=services.CreateScope()) {var p=scope.ServiceProvider.GetRequiredService<PaymentService>();draft=await p.CreateDraftAsync("pilot",saved,18,saved.TrackingToken);await p.AttachSessionAsync(draft.Token,"cs_test_verified");}
Session Session(string status="paid",long amount=1800)=>new(){Id="cs_test_verified",Mode="payment",Currency="eur",PaymentStatus=status,AmountTotal=amount,Livemode=false,Metadata=new(){{"pendingToken",draft.Token}}};
foreach(var bad in new[]{Session("unpaid"),Session(amount:1)}) {using var scope=services.CreateScope();await Reject(()=>scope.ServiceProvider.GetRequiredService<PaymentService>().FulfilAsync(bad));}
var tokens=await Task.WhenAll(Enumerable.Range(0,6).Select(async _=>{using var scope=services.CreateScope();return (await scope.ServiceProvider.GetRequiredService<PaymentService>().FulfilAsync(Session())).TrackingToken;}));
Check(tokens.Distinct().Count()==1&&tokens[0]==saved.TrackingToken,"Payment fulfilment duplicated an order");
using(var scope=services.CreateScope()) {
 var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();var order=await db.Orders.SingleAsync(x=>x.TrackingToken==saved.TrackingToken);
 Check(order.Total==18&&order.PaymentIsTest&&order.AgreedDeliveryWindow=="12:00–15:00","Payment snapshot wrong");
 Check(await db.EmailMessages.CountAsync(x=>x.To==saved.ContactEmail)==1,"Confirmation email duplicated");
 await scope.ServiceProvider.GetRequiredService<OrderService>().UpdateStatusAsync(order.Id,OrderStatus.Collected);
 await scope.ServiceProvider.GetRequiredService<OrderService>().UpdateStatusAsync(order.Id,OrderStatus.Delivered);
 Check((await db.Orders.FindAsync(order.Id))!.DeliveredAt is not null,"Delivery status not persisted");
 var archive=await scope.ServiceProvider.GetRequiredService<BackupService>().ExportAsync();
 using var zip=new ZipArchive(new MemoryStream(archive));Check(zip.GetEntry("restore.sql") is not null,"Backup missing database");
 using var reader=new StreamReader(zip.GetEntry("restore.sql")!.Open());await File.WriteAllTextAsync("/tmp/tippsend-restore.sql",await reader.ReadToEndAsync());
}
Console.WriteLine("PASS: PostgreSQL persistence, cross-instance capacity, enquiry storage, unpaid/mismatched payment rejection, concurrent webhook/return idempotency, queued email, delivery tracking, backup export.");
void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
async Task Reject(Func<Task<Order>> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Invalid payment accepted");}
class TestEnvironment:IWebHostEnvironment {
 public string EnvironmentName{get;set;}="Staging";public string ApplicationName{get;set;}="Test";public string ContentRootPath{get;set;}=Path.GetTempPath();
 public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}=".";public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
}
