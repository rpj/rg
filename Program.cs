using System;
using System.Net;
using Roentgenium.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Roentgenium
{
    public class Program
    {
        private static readonly IKeyVault KeyVault = new AzureKeyVault();
        public static void Main(string[] args)
        {
            Console.WriteLine($"{BuiltIns.Name} v{BuiltIns.Version} starting...");
#if DEBUG
            Console.WriteLine($"Built-in Specification types:\n\t{string.Join("\n\t", BuiltIns.SupportedSpecs)}");
            Console.WriteLine($"Built-in Filter types:\n\t{string.Join("\n\t", BuiltIns.SupportedFilters)}");
            Console.WriteLine($"Built-in Output sink types:\n\t{string.Join("\n\t", BuiltIns.OutputSinks.Keys)}");
#endif
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((context, config) =>
                {
                    config.AddConsole();

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddEventSourceLogger();
#if DEBUG
                        config.AddDebug();
#endif
                    }
                })
                .ConfigureAppConfiguration((context, config) => 
                    KeyVault.AddKeyVaultToBuilder(config))
                .ConfigureServices((context, services) =>
                    services.AddSingleton(KeyVault))
                .UseKestrel((ctx, options) =>  
                {  
                    IPAddress ipAddr = IPAddress.Loopback;
                    var hName = ctx.Configuration.GetValue<string>("Host:Name");
                    if (hName == "localhost" || hName == "loopback" ||
                        IPAddress.TryParse(hName, out ipAddr))
                    {
                        var secure = ctx.Configuration.GetValue<bool?>("Host:Secure");
                        var hPort = ctx.Configuration.GetValue<int?>("Host:Port");

                        if (ipAddr != IPAddress.IPv6None && secure.HasValue && hPort.HasValue)
                        {
                            Console.WriteLine("Using detected host config: " + 
                                $"http{(secure.Value ? "s" : "")}://{ipAddr}:{hPort.Value}");
                            options.Listen(ipAddr, hPort.Value, lOpts => 
                            { 
                                if (secure.Value)
                                    lOpts.UseHttps();
                            });
                        }
                    }
                }) 
                .UseStartup<Startup>();
    }
}
