using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace SignalRSvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        const string URLS = "http://0.0.0.0:15000;https://0.0.0.0:15001";

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(URLS)
                .UseStartup<Startup>();
    }
}
