using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CleanService;
using CleanService.Services;
using System.ServiceProcess;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("D:\\SideProject\\CleanService\\Logs\\log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("服務啟動中...");
    Host.CreateDefaultBuilder(args)
       .UseWindowsService() //  用在 IHostBuilder 上
       .UseSerilog()        // Serilog 
       .ConfigureServices((context, services) =>
       {
           services.AddHostedService<FileClean>();
       })
       .Build()
       .Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服務啟動失敗");
}
finally
{
    Log.CloseAndFlush();
}
