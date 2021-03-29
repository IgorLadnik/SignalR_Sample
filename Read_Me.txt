
Please replace %rootDir% with your solution directory
=====================================================


*** IIS Express Configuration

<site name="SignalRClientSite" id="15">
	<application path="/" applicationPool="Clr4IntegratedAppPool">
		<virtualDirectory path="/" physicalPath="%rootDir%\SignalRSvc\wwwroot" />
	</application>
	<bindings>
		<binding protocol="http" bindingInformation="*:15015:localhost" />
		<binding protocol="https" bindingInformation="*:44399:localhost" />
	</bindings>
</site>


*** To start "SignalRClientSite" site with IIS Express from command line:

cd C:\Program Files (x86)\IIS Express
iisexpress /site:SignalRClientSite


*** To run .NET Core server with it project related files from command line:

cd %rootDir%\SignalRSvc
dotnet run


*** To run .NET Core client with its project related files from command line:

cd %rootDir%\SignalRClientTest
dotnet run


