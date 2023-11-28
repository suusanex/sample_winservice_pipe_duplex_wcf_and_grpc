using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using gRPCWinServiceSample;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NLog;
using NLog.Extensions.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddNLog();
        logging.AddConsole();
        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information); //TODO:外部レジストリ設定の読み込みによる可変設定
        logging.AddFilter("Grpc", Microsoft.Extensions.Logging.LogLevel.Information);
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
        });
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        logger.Info($"Add WebHost Startup");
        //webBuilder.UseUrls("http://localhost:50100");

        webBuilder.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 50100, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
            options.ListenNamedPipe("gRPCWinServiceSamplePipeName", listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                g_PipeName = listenOptions.EndPoint.ToString();
            });

        });

        webBuilder.Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<WindowsServiceToUserSessionGrpcServer>();

                endpoints.MapGet("/Connect1/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<WindowsServiceToUserSessionGrpcServiceNoStreamServer>();

                endpoints.MapGet("/Connect1NoStream/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<WindowsServiceToUserSessionType2GrpcServer>();

                endpoints.MapGet("/Connect2/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });

            logger.Info($"Configure End");

        });
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();

public partial class Program
{
    private static Logger logger = LogManager.GetCurrentClassLogger();
    internal static string? g_PipeName = null;

}