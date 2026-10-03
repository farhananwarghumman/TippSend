"""Disposable CI HTTP checks; never run against a live domain."""
import http.client, urllib.parse, subprocess, time, os
from html.parser import HTMLParser
class Inputs(HTMLParser):
    def __init__(self):super().__init__();self.values={}
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if tag=='input' and 'name' in a:self.values[a['name']]=a.get('value','')
cookies={}
def request(path,data=None):
    connection=http.client.HTTPConnection('127.0.0.1',8080)
    headers={'X-Forwarded-Proto':'https','Cookie':'; '.join(k+'='+v for k,v in cookies.items())}
    body=urllib.parse.urlencode(data) if data is not None else None
    if body is not None:headers['Content-Type']='application/x-www-form-urlencoded'
    connection.request('POST' if data is not None else 'GET',path,body,headers)
    response=connection.getresponse(); text=response.read().decode()
    for name,value in response.getheaders():
        if name.lower()=='set-cookie':
            k,v=value.split(';',1)[0].split('=',1);cookies[k]=v
    status=response.status;location=response.getheader('Location');connection.close();return status,location,text
process=subprocess.Popen(['dotnet','/tmp/tippsend-publish/TippSendApp.dll'],stdout=open('/tmp/tippsend-http.log','w'),stderr=subprocess.STDOUT)
try:
    for _ in range(40):
        try:
            if request('/health')[0]==200:break
        except OSError:pass
        time.sleep(1)
    assert request('/Admin')[0]==302
    status,_,html=request('/Setup?token=disposable-ci-setup-token');assert status==200
    fields=Inputs();fields.feed(html)
    data={'__RequestVerificationToken':fields.values['__RequestVerificationToken'],'Token':'disposable-ci-setup-token','Password':'DisposableCIpassphrase123','ConfirmPassword':'DisposableCIpassphrase123'}
    assert request('/Setup',data)[0]==302
    assert request('/Setup?token=disposable-ci-setup-token')[0]==404
    _,_,html=request('/Identity/Account/Login');fields=Inputs();fields.feed(html)
    assert request('/Identity/Account/Login',{'__RequestVerificationToken':fields.values['__RequestVerificationToken'],'Input.Email':'admin@example.invalid','Input.Password':'DisposableCIpassphrase123'})[0]==302
    assert request('/Admin')[0]==200
    for path in ['/Admin/Dispatch','/Admin/Enquiries','/Admin/Emails','/Admin/Backup','/Operator/Dashboard']:
        assert request(path)[0]==200,path
    _,_,html=request('/Send');fields=Inputs();fields.feed(html)
    import datetime
    day=datetime.date.today()+datetime.timedelta(days=7)
    while day.weekday()!=5:day+=datetime.timedelta(days=1)
    data={'__RequestVerificationToken':fields.values['__RequestVerificationToken'],'Input.Service':'Dedicated','Input.PickupAddress':'Test home','Input.PickupEircode':'E91 A123','Input.DropoffAddress':'Test recipient home','Input.DropoffEircode':'E91 B123','Input.ItemDescription':'CI parcel','Input.WeightKg':'1','Input.DeclaredValue':'10','Input.ContactName':'CI sender','Input.ContactEmail':'ci@example.invalid','Input.ContactPhone':'0871234567','Input.RecipientName':'CI recipient','Input.RecipientPhone':'0877654321','Input.PreferredDate':day.isoformat(),'Input.PreferredTime':'Flexible','Input.Eligible':'true'}
    status,location,html=request('/Send',data);assert status==302,html
    assert '/Send/Request' in location
    status,_,html=request(location);assert status==200 and 'TS-R-' in html and 'Requested' in html
    print('PASS: admin setup once, login/access, admin/dispatch/enquiries/backup pages, CSRF-protected home-to-home request and private link.')
finally:
    process.terminate();process.wait(timeout=15)
