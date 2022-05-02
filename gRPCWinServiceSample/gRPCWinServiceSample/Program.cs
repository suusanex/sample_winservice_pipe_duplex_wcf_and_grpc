using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Extensions.Hosting;

namespace gRPCWinServiceSample
{
    public class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            logger.Info($"Main, {string.Join(" ", args ?? new string[0])}");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseNLog()
                .ConfigureServices((hostContext, services) =>
                {
                    logger.Info($"Add Worker");
                    services.AddHostedService<Worker>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    logger.Info($"Add WebHost Startup");
                    webBuilder.UseUrls("http://localhost:50100");
                    webBuilder.UseStartup<Startup>();
                })
                .UseWindowsService();
    }
}
